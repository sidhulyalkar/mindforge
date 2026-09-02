#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.Tests.Editor
{
    public sealed class CombatEmbodimentV27SmokeTests
    {
        [Test]
        public void V27GuardianEmbodiment_CanBeConstructedByUnity()
        {
            GameObject go = new GameObject("V27GuardianEmbodimentSmoke");
            go.SetActive(false);
            try
            {
                GuardianCombatEmbodimentV27 embodiment = go.AddComponent<GuardianCombatEmbodimentV27>();
                Assert.IsNotNull(embodiment);
                Assert.AreEqual("GuardianCombatEmbodimentV27", GuardianCombatEmbodimentV27.RootName);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void V27FracturedBeast_CanBeConstructedBesideExistingDirector()
        {
            GameObject go = new GameObject("V27FracturedBeastSmoke");
            go.SetActive(false);
            try
            {
                FracturedSignalDirector director = go.AddComponent<FracturedSignalDirector>();
                FracturedSignalBeastV27 beast = go.AddComponent<FracturedSignalBeastV27>();
                Assert.IsNotNull(director);
                Assert.IsNotNull(beast);
                Assert.AreEqual("FracturedSignalBeastV27", FracturedSignalBeastV27.RootName);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void V27ArenaDynamics_CarriesNoPhysicsRequirement()
        {
            GameObject go = new GameObject("V27ArenaDynamicsSmoke");
            go.SetActive(false);
            try
            {
                FracturedArenaDynamicsV27 dynamics = go.AddComponent<FracturedArenaDynamicsV27>();
                Assert.IsNotNull(dynamics);
                Assert.IsNull(go.GetComponent<Collider>());
                Assert.IsNull(go.GetComponent<Rigidbody>());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
