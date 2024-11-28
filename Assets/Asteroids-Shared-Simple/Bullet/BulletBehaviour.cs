using Fusion;
using UnityEngine;

namespace Asteroids.SharedSimple
{
	// Defines how the bullet behaves
	public class BulletBehaviour : NetworkBehaviour
	{
		// The settings
		[Tooltip("Maximum time for a bullet to remain alive")]
		[SerializeField] private float _maxLifetime = 3.0f;
		[Tooltip("Movement speed of bullets")]
		[SerializeField] private float _speed = 200.0f;
		[Tooltip("Collision layer for bullets")]
		[SerializeField] private LayerMask _asteroidLayer;

		[Tooltip("FX spawned when bullet detonates")]
		[SerializeField] private ParticleFx _impactFx;

		
		// The countdown for a bullet lifetime. This is networked in case the StateAuthority changes (due to disconnect)
		[Networked] private TickTimer _currentLifetime { get; set; }


		public override void Spawned()
		{
			if (Object.HasStateAuthority == false) return;

			// The network parameters get initializes by the host. These will be propagated to the clients since the
			// variables are [Networked]
			_currentLifetime = TickTimer.CreateFromSeconds(Runner, _maxLifetime);
		}
		
		public override void FixedUpdateNetwork()
		{
			// If the bullet has not hit an asteroid, moves forward.
			if (HasHitAsteroid() == false)
			{
				transform.Translate(transform.forward * _speed * Runner.DeltaTime, Space.World);
			}
			else
			{
				Runner.Despawn(Object);
				return;
			}

			CheckLifetime();
		}

		// If the bullet has exceeded its lifetime, it gets destroyed
		private void CheckLifetime()
		{
			if (_currentLifetime.Expired(Runner) == false) return;

			Runner.Despawn(Object);
		}


		// Check if the bullet will hit an asteroid in the next tick.
		public bool HasHitAsteroid()
		{
			if (Runner.GetPhysicsScene().Raycast(transform.position, transform.forward, out var hit, _speed * Runner.DeltaTime * 1.25f, _asteroidLayer))
			{
				var asteroidBehaviour = hit.collider.GetComponent<AsteroidBehaviour>();
				if (asteroidBehaviour)
				{
					var hasHit = asteroidBehaviour.OnAsteroidHit();
					if (!hasHit) return false;
					
					if (Runner.TryGetPlayerObject(Object.InputAuthority, out var playerNetworkObject))
					{
						playerNetworkObject.GetComponent<PlayerDataNetworked>().AddToScore(asteroidBehaviour.Points);
					}
					return true;
				}
			}
			return false;
		}
	}
}