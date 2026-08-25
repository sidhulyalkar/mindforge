# Scientific and hardware references

These references motivate the current Dual Aura engineering choices. They do not replace Mindforge's own physical validation.

## Moving visual BCI targets

- **“A P300-based brain-computer interface with stimuli on moving objects”**. PubMed PMID 24302977. The study demonstrated P300 target selection with freely moving stimuli.
  - https://pubmed.ncbi.nlm.nih.gov/24302977/

- **“A Novel SSVEP Brain-Computer Interface System Based on Simultaneous Modulation of Luminance and Motion”**. PubMed PMID 37022370. A moving-flicker SSVEP study using FBCCA; performance decreased as superimposed motion became faster, supporting Mindforge's deliberately slow orbit design.
  - https://pubmed.ncbi.nlm.nih.gov/37022370/

- **“Comparisons of stimulus paradigms for SSVEP-based brain-computer interfaces”**. PubMed PMID 40245876 (2025). Compares flicker, motion-VEP, and frame-rate video paradigms including user experience.
  - https://pubmed.ncbi.nlm.nih.gov/40245876/

## SSVEP comfort / frequency design

- **“Optimizing Stimulus Frequency Ranges for Building a High-Rate High Frequency SSVEP-BCI”**. PubMed PMID 37022899. Demonstrates the tradeoff between high-frequency stimulation, comfort, and performance.
  - https://pubmed.ncbi.nlm.nih.gov/37022899/

## Unicorn Hybrid Black

- g.tec Unicorn Hybrid Black official product information: 8 EEG channels, 24-bit resolution, 250 Hz/channel, hybrid wet/dry electrodes, Bluetooth, accelerometer/gyroscope, and development interfaces for BCI applications.
  - https://www.gtec.at/product/unicorn-hybrid-black-bci-platform/

- g.tec Unicorn Suite official information: C/.NET APIs and optional Unity, Python, LSL and UDP integration routes.
  - https://www.gtec.at/product/unicorn-suite/

- g.tec Unicorn Hybrid Black Quick Start Guide: includes practical setup/noise guidance and recommends filtering appropriate to EEG acquisition.
  - https://www.gtec.at/wp-content/uploads/2023/10/web-unicorn-hybrid-black-hello-quickstart-2019-02-06.pdf

## Montage

The standard Unicorn Hybrid Black cap is commonly documented with channels:

```text
Fz, C3, Cz, C4, Pz, PO7, Oz, PO8
```

For Dual Aura SSVEP, the posterior subset `Pz/PO7/Oz/PO8` is the primary starting point. Channel weighting/selection must be learned or validated from actual sessions rather than assumed from anatomy alone.
