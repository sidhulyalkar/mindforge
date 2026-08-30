// Adapted from Maxim Gumin's WaveFunctionCollapse (MIT), 2016.
// Mindforge uses the entropy-guided collapse + propagation ideas only; no upstream art assets.
using System;
using System.Collections.Generic;

namespace Mindforge.ThirdParty.Wfc
{
    /// <summary>
    /// Compact 2D constraint-collapse solver for small authored tile catalogs.
    /// Adjacency is [direction, sourceTile, neighborTile] where directions are
    /// north/east/south/west. The implementation intentionally favors readability and
    /// deterministic editor/runtime generation over the upstream solver's maximal throughput.
    /// </summary>
    public sealed class MindforgeConstraintCollapse
    {
        private static readonly int[] Dx = { 0, 1, 0, -1 };
        private static readonly int[] Dy = { 1, 0, -1, 0 };
        private static readonly int[] Opposite = { 2, 3, 0, 1 };

        private readonly int _width;
        private readonly int _height;
        private readonly double[] _weights;
        private readonly bool[,,] _adjacency;
        private readonly bool[][] _wave;
        private readonly Queue<int> _queue = new Queue<int>();
        private readonly bool[] _queued;
        private int[] _observed;

        public MindforgeConstraintCollapse(int width, int height, double[] weights, bool[,,] adjacency)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (weights == null || weights.Length == 0) throw new ArgumentException("At least one tile weight is required.", nameof(weights));
            if (adjacency == null || adjacency.GetLength(0) != 4 || adjacency.GetLength(1) != weights.Length || adjacency.GetLength(2) != weights.Length)
                throw new ArgumentException("Adjacency must have shape [4,tileCount,tileCount].", nameof(adjacency));

            _width = width;
            _height = height;
            _weights = new double[weights.Length];
            for (int i = 0; i < weights.Length; i++) _weights[i] = Math.Max(0.000001, weights[i]);
            _adjacency = adjacency;
            _wave = new bool[width * height][];
            _queued = new bool[_wave.Length];
            for (int i = 0; i < _wave.Length; i++) _wave[i] = new bool[_weights.Length];
        }

        public bool Run(int seed, out int[] observed)
        {
            Clear();
            Random random = new Random(seed);

            while (true)
            {
                int cell = NextUnobservedCell(random);
                if (cell < 0)
                {
                    _observed = new int[_wave.Length];
                    for (int i = 0; i < _wave.Length; i++)
                    {
                        _observed[i] = FirstAllowed(_wave[i]);
                        if (_observed[i] < 0)
                        {
                            observed = null;
                            return false;
                        }
                    }
                    observed = (int[])_observed.Clone();
                    return true;
                }

                Collapse(cell, random);
                if (!Propagate())
                {
                    observed = null;
                    return false;
                }
            }
        }

        private void Clear()
        {
            _queue.Clear();
            Array.Clear(_queued, 0, _queued.Length);
            for (int i = 0; i < _wave.Length; i++)
                for (int t = 0; t < _weights.Length; t++)
                    _wave[i][t] = true;
            _observed = null;
        }

        private int NextUnobservedCell(Random random)
        {
            double min = double.PositiveInfinity;
            int best = -1;
            for (int i = 0; i < _wave.Length; i++)
            {
                int count = CountAllowed(_wave[i]);
                if (count == 0) return i;
                if (count == 1) continue;

                double sum = 0.0;
                double sumWeightLogWeight = 0.0;
                for (int t = 0; t < _weights.Length; t++)
                {
                    if (!_wave[i][t]) continue;
                    double w = _weights[t];
                    sum += w;
                    sumWeightLogWeight += w * Math.Log(w);
                }
                double entropy = Math.Log(sum) - sumWeightLogWeight / sum;
                double noisyEntropy = entropy + random.NextDouble() * 1e-6;
                if (noisyEntropy < min)
                {
                    min = noisyEntropy;
                    best = i;
                }
            }
            return best;
        }

        private void Collapse(int cell, Random random)
        {
            bool[] possibilities = _wave[cell];
            double total = 0.0;
            for (int t = 0; t < possibilities.Length; t++)
                if (possibilities[t]) total += _weights[t];

            if (total <= 0.0)
            {
                for (int t = 0; t < possibilities.Length; t++) possibilities[t] = false;
                Enqueue(cell);
                return;
            }

            double cursor = random.NextDouble() * total;
            int chosen = -1;
            for (int t = 0; t < possibilities.Length; t++)
            {
                if (!possibilities[t]) continue;
                cursor -= _weights[t];
                if (cursor <= 0.0)
                {
                    chosen = t;
                    break;
                }
            }
            if (chosen < 0) chosen = FirstAllowed(possibilities);

            for (int t = 0; t < possibilities.Length; t++) possibilities[t] = t == chosen;
            Enqueue(cell);
        }

        private bool Propagate()
        {
            while (_queue.Count > 0)
            {
                int cell = _queue.Dequeue();
                _queued[cell] = false;
                int x = cell % _width;
                int y = cell / _width;

                for (int direction = 0; direction < 4; direction++)
                {
                    int nx = x + Dx[direction];
                    int ny = y + Dy[direction];
                    if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;

                    int neighbor = nx + ny * _width;
                    bool changed = false;
                    bool[] sourceWave = _wave[cell];
                    bool[] neighborWave = _wave[neighbor];

                    for (int neighborTile = 0; neighborTile < neighborWave.Length; neighborTile++)
                    {
                        if (!neighborWave[neighborTile]) continue;
                        bool supported = false;
                        for (int sourceTile = 0; sourceTile < sourceWave.Length; sourceTile++)
                        {
                            if (!sourceWave[sourceTile]) continue;
                            if (_adjacency[direction, sourceTile, neighborTile] &&
                                _adjacency[Opposite[direction], neighborTile, sourceTile])
                            {
                                supported = true;
                                break;
                            }
                        }
                        if (supported) continue;
                        neighborWave[neighborTile] = false;
                        changed = true;
                    }

                    int remaining = CountAllowed(neighborWave);
                    if (remaining == 0) return false;
                    if (changed) Enqueue(neighbor);
                }
            }
            return true;
        }

        private void Enqueue(int cell)
        {
            if (_queued[cell]) return;
            _queued[cell] = true;
            _queue.Enqueue(cell);
        }

        private static int CountAllowed(bool[] values)
        {
            int count = 0;
            for (int i = 0; i < values.Length; i++) if (values[i]) count++;
            return count;
        }

        private static int FirstAllowed(bool[] values)
        {
            for (int i = 0; i < values.Length; i++) if (values[i]) return i;
            return -1;
        }
    }
}
