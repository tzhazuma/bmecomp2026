using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class VisualFeedback : MonoBehaviour
{
    public float lowAttentionThreshold = 40f;
    public float highAttentionThreshold = 70f;
    public float comboThreshold = 10f;
    public float transitionSpeed = 3f;

    public PostProcessVolume postProcessVolume;
    public ParticleSystem comboParticleEffect;

    public AnimationCurve vignetteCurve = AnimationCurve.Linear(0, 0.6f, 1, 0);
    public AnimationCurve saturationCurve = AnimationCurve.Linear(0, 0.3f, 1, 1.1f);
    public AnimationCurve bloomCurve = AnimationCurve.Linear(0, 0, 0.7f, 0, 1, 0.8f);

    private Vignette vignette;
    private ColorGrading colorGrading;
    private Bloom bloom;

    private BCIManager bciManager;
    private float currentAttention = 50f;
    private float comboProgress = 0f;
    private bool isComboActive = false;

    void Start()
    {
        bciManager = BCIManager.Instance;
        if (postProcessVolume)
        {
            postProcessVolume.profile.TryGetSettings(out vignette);
            postProcessVolume.profile.TryGetSettings(out colorGrading);
            postProcessVolume.profile.TryGetSettings(out bloom);
        }
        if (comboParticleEffect) comboParticleEffect.Stop();
    }

    void Update()
    {
        if (bciManager)
            currentAttention = Mathf.Lerp(currentAttention, bciManager.GetAttention(), Time.deltaTime * transitionSpeed);

        UpdatePostProcessing();
        UpdateCombo();
    }

    private void UpdatePostProcessing()
    {
        float ratio = Mathf.Clamp01(currentAttention / 100f);

        if (vignette)
            vignette.intensity.value = vignetteCurve.Evaluate(ratio);
        if (colorGrading)
            colorGrading.saturation.value = saturationCurve.Evaluate(ratio) * 100f - 100f;
        if (bloom)
            bloom.intensity.value = bloomCurve.Evaluate(ratio);
    }

    private void UpdateCombo()
    {
        if (currentAttention >= highAttentionThreshold)
        {
            comboProgress += Time.deltaTime;
            if (comboProgress >= comboThreshold && !isComboActive)
                ActivateCombo();
        }
        else
        {
            if (isComboActive)
                DeactivateCombo();
            comboProgress = 0f;
        }
    }

    private void ActivateCombo()
    {
        isComboActive = true;
        if (comboParticleEffect) comboParticleEffect.Play();
        if (bloom) bloom.intensity.value = 1.5f;
        Debug.Log("专注连击激活!");
    }

    private void DeactivateCombo()
    {
        isComboActive = false;
        if (comboParticleEffect) comboParticleEffect.Stop();
        Debug.Log("专注连击结束");
    }

    public float GetComboProgress() => Mathf.Clamp01(comboProgress / comboThreshold);
    public bool IsComboActive() => isComboActive;
    public float GetCurrentAttentionDisplay() => currentAttention;
}
