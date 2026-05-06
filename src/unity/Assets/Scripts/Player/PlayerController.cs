using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float baseSpeed = 10f;
    public float maxSpeed = 30f;
    public float minSpeed = 5f;
    public float yawSensitivity = 2f;
    public float pitchSensitivity = 1.5f;
    public float imuDeadzone = 5f;
    public float horizontalLimit = 20f;
    public float verticalLimit = 10f;
    public float speedSmoothTime = 3f;
    public float imuSmoothTime = 0.1f;

    public Transform cameraTransform;
    public ParticleSystem shipTrail;
    public GameObject shieldEffect;

    [Header("Camera Shake")]
    public float shakeIntensity = 0.3f;
    public float shakeSpeed = 5f;

    private float currentSpeed;
    private float targetSpeed;
    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;
    private float shieldStrength = 0f;

    private BCIManager bciManager;
    private Vector3 startCameraPos;
    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.position;
        bciManager = BCIManager.Instance;
        startCameraPos = cameraTransform ? cameraTransform.localPosition : Vector3.zero;

        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
            bciManager.OnIMUUpdated += OnIMUUpdated;
        }
    }

    void Update()
    {
        UpdateSpeed();
        UpdatePosition();
        UpdateShield();
        UpdateCameraShake();
    }

    private void UpdateSpeed()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedSmoothTime);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    private void UpdatePosition()
    {
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime / (imuSmoothTime + 0.001f));
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime / (imuSmoothTime + 0.001f));

        float horizontalMove = currentYaw * yawSensitivity * Time.deltaTime;
        float verticalMove = currentPitch * pitchSensitivity * Time.deltaTime;

        Vector3 newPos = transform.position + new Vector3(horizontalMove, verticalMove, 0);
        newPos.x = Mathf.Clamp(newPos.x, originalPosition.x - horizontalLimit, originalPosition.x + horizontalLimit);
        newPos.y = Mathf.Clamp(newPos.y, originalPosition.y - verticalLimit, originalPosition.y + verticalLimit);
        transform.position = newPos;
    }

    private void UpdateShield()
    {
        if (shieldEffect)
        {
            bool shieldActive = shieldStrength > 0.5f;
            if (shieldEffect.activeSelf != shieldActive)
                shieldEffect.SetActive(shieldActive);
            if (shieldActive)
            {
                float scale = 1f + shieldStrength * 0.5f;
                shieldEffect.transform.localScale = Vector3.one * scale;
                var main = shieldEffect.GetComponent<ParticleSystem>().main;
                main.startColor = Color.Lerp(Color.cyan, Color.gold, shieldStrength);
            }
        }
    }

    private void UpdateCameraShake()
    {
        if (!cameraTransform) return;
        if (shieldStrength < 0.4f)
        {
            float intensity = (0.4f - shieldStrength) * shakeIntensity * 2f;
            float offsetX = Mathf.PerlinNoise(Time.time * shakeSpeed, 0) * 2 - 1;
            float offsetY = Mathf.PerlinNoise(0, Time.time * shakeSpeed) * 2 - 1;
            cameraTransform.localPosition = startCameraPos + new Vector3(offsetX * intensity, offsetY * intensity, 0);
        }
        else
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, startCameraPos, Time.deltaTime * 3f);
        }
    }

    private void OnAttentionUpdated(float attention)
    {
        float ratio = Mathf.Clamp01(attention / 100f);
        targetSpeed = minSpeed + (maxSpeed - minSpeed) * ratio;
        shieldStrength = Mathf.Clamp01((attention - 30f) / 60f);

        if (shipTrail)
        {
            var main = shipTrail.main;
            main.startSpeedMultiplier = 1f + ratio;
            main.startColor = Color.Lerp(Color.gray, Color.gold, ratio);
        }
    }

    private void OnIMUUpdated(float yaw, float pitch, float roll, float sx, float sy)
    {
        if (Mathf.Abs(yaw) < imuDeadzone) yaw = 0f;
        if (Mathf.Abs(pitch) < imuDeadzone) pitch = 0f;
        targetYaw = yaw;
        targetPitch = pitch;
    }

    void OnDestroy()
    {
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
            bciManager.OnIMUUpdated -= OnIMUUpdated;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (shieldStrength > 0.5f)
            {
                GameManager.Instance?.OnShieldBlockObstacle();
                Destroy(collision.gameObject);
            }
            else
            {
                GameManager.Instance?.OnPlayerHitObstacle();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            GameManager.Instance?.OnCollectiblePickedUp();
            Destroy(other.gameObject);
        }
    }

    public float GetSpeedRatio() => Mathf.Clamp01(currentSpeed / maxSpeed);
    public float GetShieldStrength() => shieldStrength;
    public float GetCurrentSpeed() => currentSpeed;
}
