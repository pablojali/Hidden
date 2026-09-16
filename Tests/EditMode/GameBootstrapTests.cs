using Hidden.Core;
using NUnit.Framework;
using UnityEngine;

namespace Hidden.Tests
{
    public class GameBootstrapTests
    {
        [Test]
        public void Awake_SetsConfiguredTargetFrameRate()
        {
            var go = new GameObject("Bootstrap");

            try
            {
                go.AddComponent<GameBootstrap>();

                Assert.AreEqual(60, Application.targetFrameRate);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
