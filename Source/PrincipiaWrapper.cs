using System;
using System.Collections.Generic;

namespace HyperEdit
{
    public static class PrincipiaWrapper
    {
        static bool PrincipiaInstalled;
        static readonly Dictionary<CelestialBody, double> originalGeeForces = new Dictionary<CelestialBody, double>();

        public static bool Init()
        {
            Extensions.Log("Attempting to find Principia....");

            foreach (AssemblyLoader.LoadedAssembly assembly in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    if (assembly.assembly.GetName().Name == "principia.ksp_plugin_adapter")
                    {
                        Extensions.Log($"Principia found.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Extensions.Log($"Error loading Principia: {ex}");
                }
            }

            Extensions.Log($"Principia not found.");

            return false;
        }

        static PrincipiaWrapper()
        {
            PrincipiaInstalled = Init();
        }

        public static void PrincipiaHackGravity()
        {
            UnityEngine.Debug.Log("Calling PrincipiaHackGravity");
            if (PrincipiaInstalled)
            {
                originalGeeForces.Clear();
                for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
                {
                    CelestialBody celestialBody = FlightGlobals.Bodies[i];
                    originalGeeForces.Add(celestialBody, celestialBody.GeeASL);
                }
                FlightGlobals.currentMainBody.GeeASL = 0.01;
            }
        }

        public static void PrincipiaUnhackGravity()
        {
            UnityEngine.Debug.Log("Calling PrincipiaUnhackGravity");
            if (PrincipiaInstalled)
            {
                for (int j = 0; j < FlightGlobals.Bodies.Count; j++)
                {
                    CelestialBody celestialBody2 = FlightGlobals.Bodies[j];
                    celestialBody2.GeeASL = originalGeeForces[celestialBody2];
                }
            }
        }
    }
}
