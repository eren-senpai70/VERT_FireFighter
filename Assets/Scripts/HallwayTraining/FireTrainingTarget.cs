using UnityEngine;

namespace Vert.Firefighter.Training
{
    public sealed class FireTrainingTarget : MonoBehaviour
    {
        [SerializeField] GameObject[] flameVisuals;
        [SerializeField] ParticleSystem flameParticles;
        [SerializeField] HallwayTrainingPrompt prompt;
        [SerializeField] float extinguishSeconds = 3f;

        float suppression;
        int lastReportedProgress = -1;
        public bool IsExtinguished { get; private set; }

        public void Configure(GameObject[] visuals, ParticleSystem particles, HallwayTrainingPrompt trainingPrompt)
        {
            flameVisuals = visuals;
            flameParticles = particles;
            prompt = trainingPrompt;
        }

        public void ApplySuppressant(float deltaTime)
        {
            if (IsExtinguished)
                return;

            suppression += deltaTime;
            if (suppression >= extinguishSeconds)
            {
                IsExtinguished = true;
                if (flameParticles != null)
                    flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (flameVisuals != null)
                {
                    foreach (var visual in flameVisuals)
                    {
                        if (visual != null)
                            visual.SetActive(false);
                    }
                }

                if (prompt != null)
                    prompt.SetText("Fire out! Training complete.");
            }
            else if (prompt != null)
            {
                var progress = Mathf.Clamp01(suppression / extinguishSeconds);
                var percent = Mathf.RoundToInt(progress * 100f);
                if (percent != lastReportedProgress)
                {
                    lastReportedProgress = percent;
                    prompt.SetText($"Keep aiming at the fire... {percent}%\nKeep holding the trigger to spray.");
                }
            }
        }
    }
}
