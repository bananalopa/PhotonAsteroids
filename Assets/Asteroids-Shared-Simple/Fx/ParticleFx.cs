using UnityEngine;

namespace Asteroids.SharedSimple
{
	// Simple helper to auto-release an FX game object when the associated ParticleSystem stops playing
	public class ParticleFx : MonoBehaviour
	{
		private ParticleSystem _ps;

		private void Awake()
		{
			_ps = GetComponent<ParticleSystem>();
		}

		private void OnEnable()
		{
			// This elaborate tap dance is needed to ensure the ParticleSystem plays when re-used
			_ps.Stop();
			_ps.Clear();
			_ps.Simulate(0,true,true);
			_ps.Play();
		}

		private void LateUpdate()
		{ 
			if (!_ps.isPlaying)
				Destroy(gameObject);
		}
	}
}