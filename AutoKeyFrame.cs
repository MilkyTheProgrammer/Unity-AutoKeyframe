using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class AutoKeyFrame : MonoBehaviour
{
    public AnimationClip animationClip; // Reference to the AnimationClip created in the Unity Editor
    public AnimationClip danceAnimation; // Reference to the AnimationClip created in the Unity Editor

    public Light lightSource;
    public float maxIntensity = 5f;
    public float minIntensity = 0f;
    public float intensityMultiplier = 1.5f;
    public static float threshold = 4f;
    public GameObject Object;
    public Material material;
    public ParticleSystem particleSystem;

    // Option 2: Get the material programmatically

    private float[] _spectrum;
    private float _lastBeatTime;
    private bool _isBeat;
    private bool _isRecording;

    public static float bpm = 105f;
    public static float timePerBeat;
    public static float nextBeatTime;

    public GameObject cube;
    private Vector3 initialScale;

    private AnimationCurve _animationCurve;

    public float magnificationMultiplier = 1f;
    public float screenRotationMultiplier = 0.4f;
    public float sizeGirlsMultiplier = 2f;
    public float neonBrightnessMultiplier = 12f;

    private AnimationCurve magnificationCurve;
    private AnimationCurve screenRotationCurve;
    private AnimationCurve sizeGirlsCurve;
    private AnimationCurve neonBrightnessCurve;
    private AnimationCurve shakeCurve;
    public List<string> properties;

    void OnApplicationQuit()
    {
        Debug.Log("Saving animation clip, this may freeze unity for a bit...");
        StopRecordingAndSaveClip();
    }

    void OnGUI()
    {
        properties = new List<string>();
        int propertyCount = material.shader.GetPropertyCount();

        // Iterate through each shader property
        for (int i = 0; i < propertyCount; i++)
        {
            // Get the name of the shader property at index i
            string propertyName = material.shader.GetPropertyName(i);
            int propertyIndex = material.shader.FindPropertyIndex(propertyName);

            // Get the type of the shader property at index i
            ShaderPropertyType propertyType = material.shader.GetPropertyType(propertyIndex);
            properties.Add(propertyName);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

        _spectrum = new float[512];
        _lastBeatTime = Time.time - 1f; // initialize to a time just before the first update
        initialScale = cube.transform.localScale;

        _animationCurve = new AnimationCurve();
        _isRecording = true;
        StartRecording();
        magnificationCurve = new AnimationCurve();
        screenRotationCurve = new AnimationCurve();
        sizeGirlsCurve = new AnimationCurve();
        neonBrightnessCurve = new AnimationCurve();
        shakeCurve = new AnimationCurve();

    }

    // Update is called once per frame
    void Update()
    {
        // Apply Fourier transform
        AudioListener.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

        // Calculate power spectrum
        for (int i = 0; i < _spectrum.Length; i++)
        {
            _spectrum[i] = Mathf.Pow(_spectrum[i], 2);
        }

        // Apply bandpass filter
        float[] bandPass = new float[512];
        for (int i = 0; i < bandPass.Length; i++)
        {
            float frequency = i * AudioSettings.outputSampleRate / 512;
            if (frequency > 100 && frequency < 200)
            {
                bandPass[i] = _spectrum[i];
            }
            else
            {
                bandPass[i] = 0;
            }
        }

        // Find the maximum bass level
        float maxBassLevel = 0f;
        for (int i = 0; i < bandPass.Length; i++)
        {
            maxBassLevel = Mathf.Max(maxBassLevel, bandPass[i]);
        }

        float normalizedBassLevel = maxBassLevel / threshold;
        float scaledSize = Mathf.Lerp(minIntensity, maxIntensity, normalizedBassLevel);
        material.SetFloat("_Zoom", scaledSize * magnificationMultiplier);
        material.SetFloat("_Iteration", scaledSize * screenRotationMultiplier);
        material.SetFloat("_GlowStrength", scaledSize * neonBrightnessMultiplier);
        if (scaledSize * magnificationMultiplier > 0.8f)
        {
            material.SetFloat("_isSimpleScreenShakeActive", 1);
        }
        else
        {
            material.SetFloat("_isSimpleScreenShakeActive", 0);
        }

        if (_isRecording)
        {
            // Add keyframes for each material property to the animation curve
            float time = Time.time;

            magnificationCurve.AddKey(new Keyframe(time, scaledSize * magnificationMultiplier));
            screenRotationCurve.AddKey(new Keyframe(time, scaledSize * screenRotationMultiplier));
            sizeGirlsCurve.AddKey(new Keyframe(time, scaledSize * sizeGirlsMultiplier));
            neonBrightnessCurve.AddKey(new Keyframe(time, scaledSize * neonBrightnessMultiplier));
            if (scaledSize * magnificationMultiplier > 0.07f)
            {
                shakeCurve.AddKey(new Keyframe(time, 1));
            }
            else
            {
                shakeCurve.AddKey(new Keyframe(time, 0));
            }
        }
    }

    // Start recording animation
    public void StartRecording()
    {
        _animationCurve = new AnimationCurve();
        _isRecording = true;
    }

    // Stop recording and save animation clip
    public void StopRecordingAndSaveClip()
    {
        _isRecording = false;
        SaveAnimationClip();
    }

    // Save animation clip
    void SaveAnimationClip()
    {
        AnimationClip newClip = new AnimationClip();
        AnimationUtility.SetAnimationClipSettings(newClip, new AnimationClipSettings());

        // Add the recorded curves to the new animation clip
        EditorCurveBinding magnificationBinding = new EditorCurveBinding();
        magnificationBinding.path = "";
        magnificationBinding.type = typeof(MeshRenderer);
        magnificationBinding.propertyName = "material._Zoom";
        AnimationUtility.SetEditorCurve(newClip, magnificationBinding, magnificationCurve);

        EditorCurveBinding screenRotationBinding = new EditorCurveBinding();
        screenRotationBinding.path = "";
        screenRotationBinding.type = typeof(MeshRenderer);
        screenRotationBinding.propertyName = "material._Rotate";
        AnimationUtility.SetEditorCurve(newClip, screenRotationBinding, screenRotationCurve);

        EditorCurveBinding neonBrightnessBinding = new EditorCurveBinding();
        neonBrightnessBinding.path = "";
        neonBrightnessBinding.type = typeof(MeshRenderer);
        neonBrightnessBinding.propertyName = "material._GlowStrength";
        AnimationUtility.SetEditorCurve(newClip, neonBrightnessBinding, neonBrightnessCurve);

        EditorCurveBinding shakeBinding = new EditorCurveBinding();
        shakeBinding.path = "";
        shakeBinding.type = typeof(MeshRenderer);
        shakeBinding.propertyName = "material._isSimpleScreenShakeActive";
        AnimationUtility.SetEditorCurve(newClip, shakeBinding, shakeCurve);

        string filePath = "Assets/TestAnimation.anim";
        UnityEditor.AssetDatabase.CreateAsset(newClip, filePath);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("Animation clip saved at: " + filePath);
    }
}
