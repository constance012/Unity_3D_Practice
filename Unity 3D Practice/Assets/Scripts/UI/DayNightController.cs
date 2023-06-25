using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DayNightController : MonoBehaviour
{
	[Header("Time Configuration")]
	[Space]
	[SerializeField, Min(0f)] private float timeMultiplier;
	[SerializeField, Range(0f, 23f), Tooltip("Determine the start hour when entering play mode.")]
	private float timeOfDay;

	[Space]
	[SerializeField, Range(0f, 23f), Tooltip("When does the sun rise? Indicates the beginning of day.")]
	private float sunriseHour;
	
	[SerializeField, Range(0f, 23f), Tooltip("When does the sun set? Indicates the beginning of night.")] 
	private float sunsetHour;

	[Space]
	[SerializeField] private Light directionalLight;

	[Space]
	[SerializeField] private Gradient sunColor;
	[SerializeField] private Gradient ambientColor;

	// Private fields.
	private TextMeshProUGUI _dateText;
	private TextMeshProUGUI _timeText;

	private DateTime _startTime;
	private DateTime _currentTime;
	private TimeSpan _sunriseTime;
	private TimeSpan _sunsetTime;

	private int _daysPassed;
	private readonly float r_OneDayTime = (float)TimeSpan.FromHours(24f).TotalSeconds;

	private void Awake()
	{
		_dateText = transform.Find("Date Text").GetComponent<TextMeshProUGUI>();
		_timeText = transform.Find("Time Text").GetComponent<TextMeshProUGUI>();

		if (RenderSettings.sun != null)
		{
			Debug.Log("Using the asigned sun source in render settings.", this);
			directionalLight = RenderSettings.sun;
		}
		else
		{
			Debug.Log("Using the first directional light found in the scene.", this);
			Light[] lights = FindObjectsOfType<Light>();
			foreach (Light light in lights)
				if (light.type == LightType.Directional)
				{
					directionalLight = light;
					break;
				}

		}
	}

	private void Start()
	{
		_startTime = DateTime.Now.Date + TimeSpan.FromHours(timeOfDay);
		_currentTime = _startTime;

		_sunriseTime = TimeSpan.FromHours(sunriseHour);
		_sunsetTime = TimeSpan.FromHours(sunsetHour);

		SetUpGradientKeys();
	}

	private void Update()
	{
		UpdateTime();
	}

	private void LateUpdate()
	{
		ApplySunRotation();
	}

	private void UpdateTime()
	{
		_currentTime = _currentTime.AddSeconds(Time.deltaTime * timeMultiplier);
		float timeOfDayNormalized = (float)_currentTime.TimeOfDay.TotalSeconds / r_OneDayTime;

		directionalLight.color = sunColor.Evaluate(timeOfDayNormalized);
		RenderSettings.ambientLight = ambientColor.Evaluate(timeOfDayNormalized);

		_timeText.text = _currentTime.ToString("HH:mm");

		_daysPassed = (_currentTime - _startTime).Days;
		_dateText.text = $"Day {_daysPassed + 1}";
	}

	private void ApplySunRotation()
	{
		float sunEulerAngle;

		// Daytime portion of the day.
		if (_currentTime.TimeOfDay > _sunriseTime && _currentTime.TimeOfDay < _sunsetTime)
		{
			TimeSpan daytimePeriod = GetTimeDifference(_sunriseTime, _sunsetTime);
			TimeSpan timeSinceSunrise = GetTimeDifference(_sunriseTime, _currentTime.TimeOfDay);

			double daytimeNormalized = timeSinceSunrise.TotalSeconds / daytimePeriod.TotalSeconds;

			sunEulerAngle = Mathf.Lerp(0f, 180f, (float)daytimeNormalized);
		}

		// Nighttime portion of the day.
		else
		{
			TimeSpan nighttimePeriod = GetTimeDifference(_sunsetTime, _sunriseTime);
			TimeSpan timeSinceSunset = GetTimeDifference(_sunsetTime, _currentTime.TimeOfDay);

			double nighttimeNormalized = timeSinceSunset.TotalSeconds / nighttimePeriod.TotalSeconds;

			sunEulerAngle = Mathf.Lerp(180f, 360f, (float)nighttimeNormalized);
		}

		directionalLight.transform.rotation = Quaternion.Euler(sunEulerAngle, 120f, 0f);
	}

	private TimeSpan GetTimeDifference(TimeSpan fromTime, TimeSpan toTime)
	{
		TimeSpan difference = toTime - fromTime;

		// If the value is negative, it indicates that 1 day has passed.
		if (difference.TotalSeconds < 0.0)
		{
			difference += TimeSpan.FromHours(24f);
		}

		return difference;
	}

	private void SetUpGradientKeys()
	{
		GradientColorKey[] ambientColorKeys = ambientColor.colorKeys;

		// Sunrise key.
		ambientColorKeys[1].time = (float)(_sunriseTime.TotalSeconds / r_OneDayTime);

		// Mid day key.
		ambientColorKeys[2].time = (float)(((_sunriseTime.TotalSeconds + _sunsetTime.TotalSeconds) / 2f) / r_OneDayTime);

		// Sunset key.
		ambientColorKeys[3].time = (float)(_sunsetTime.TotalSeconds / r_OneDayTime);

		ambientColor.colorKeys = ambientColorKeys;
	}
}
