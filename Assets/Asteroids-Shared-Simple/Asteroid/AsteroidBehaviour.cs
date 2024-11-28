// Toggle this to use RPCs instead of the tick aligned relay.
// Asteroid impact position will be off no matter what; how much is a function of RTT.
// The difference is that the RPC is sent and executed irrespective of timing,
// so with RPCs the bullet will not disappear in the same tick as the exploding asteroid appears.

using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Asteroids.SharedSimple
{
	public class AsteroidBehaviour : NetworkBehaviour
	{
		[Tooltip("Points awarded when this asteroid is destroyed")]
		[SerializeField] private int _points = 1;

		[Tooltip("Optional new prefab to spawn when this asteroid is destroyed")]
		[SerializeField] private AsteroidBehaviour _smallAsteroid;

		[Tooltip("Destruction FX spawned when this asteroid is destroyed")]
		[SerializeField] private ParticleFx _impactFx;

		[Tooltip("Minimum number of splinters created when asteroid is destroyed")]
		[SerializeField] private int _minAsteroidSplinters = 3;

		[Tooltip("Maximum number of splinters created when asteroid is destroyed")]
		[SerializeField] private int _maxAsteroidSplinters = 6;

		[Tooltip("The visual model of the asteroid")]
		[SerializeField] private GameObject _visual;

		[Networked] private TickTimer DespawnTimer { get; set; }

		public bool IsAlive => _visual.activeSelf;
		public int Points => _points;

		private Rigidbody _rb;
		private NetworkRigidbody3D _nrb;
		private Collider _collider;

		private void Awake()
		{
			_rb = GetComponent<Rigidbody>();
			_nrb = GetComponent<NetworkRigidbody3D>();
			_collider = GetComponent<Collider>();
		}
		
		public override void Spawned()
		{
			// Reset rigidbody (it may be recycled from the pool and we don't want old inertia to stick)
			_nrb.Teleport(transform.position);
			_rb.position = transform.position;
			_rb.linearVelocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;

			// Activate the visual and the collider (in case this is a recycled Asteroid)
			_visual.SetActive(true);
			_collider.enabled = true;
		}


		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_AsteroidHitEvent(Vector3 p)
		{
			// Spawn an effect
			Instantiate(_impactFx, p, _impactFx.transform.rotation);
					
			// Hide the asteroid and disable its collider so we don't hit it again
			_visual.SetActive(false);
			_collider.enabled = false;

			// If we're the SA of this asteroid, it's our job to despawn it,
			// but we delay it a few ticks so rendering gets a chance to catch up on all peers.
			if (HasStateAuthority)
			{
				DespawnTimer = TickTimer.CreateFromTicks(Runner, 20);
				if (_smallAsteroid)
					CreateSplinters(transform.position);
			}

		}

		// Initialize the networked state of a new Asteroid. This is separate from spawned so we can pass a parameter to it.
		// Only called on StateAuthority.
		public void InitState(Vector3 force)
		{
			// Apply some random rotation to the asteroid
			Vector3 torque = Random.insideUnitSphere * Random.Range(500.0f, 1500.0f);
			_rb.AddForce(force);
			_rb.AddTorque(torque);
		}

		// When the asteroid gets hit by a player or a bullet, this method is called on the peer that detected the collision (only).
		// This is probably not the same peer that owns the Asteroid so it can't simply destroy the Asteroid.
		public bool OnAsteroidHit()
		{
			if (Object == null) return false;
			if (IsAlive == false) return false;

			RPC_AsteroidHitEvent(transform.position);
			

			return true;
		}

		public override void FixedUpdateNetwork()
		{
			// Despawn asteroid if it left the game boundaries or it was destroyed.
			if (!IsWithinScreenBoundary(transform.position) || DespawnTimer.Expired(Runner))
				Runner.Despawn(Object);
		}

		// Checks whether a position is inside the screen boundaries
		private bool IsWithinScreenBoundary(Vector3 asteroidPosition)
		{
			return Mathf.Abs(asteroidPosition.x) < GameController.Singleton.ScreenBoundaryX && Mathf.Abs(asteroidPosition.z) < GameController.Singleton.ScreenBoundaryY;
		}

		// Spawns a random amount of small asteroids when a big asteroid is destroyed.
		private void CreateSplinters(Vector3 position)
		{
			int splintersToSpawn = Random.Range(_minAsteroidSplinters, _maxAsteroidSplinters);
			
			for (int counter = 0; counter < splintersToSpawn; ++counter)
			{
				Vector3 force = Quaternion.Euler(0, counter * 360.0f / splintersToSpawn, 0) * Vector3.forward * Random.Range(0.5f, 1.5f) * 300.0f;
				Quaternion rotation = Quaternion.Euler(0, Random.value * 180.0f, 0);
			
				AsteroidBehaviour asteroid = Runner.Spawn(_smallAsteroid, position + force.normalized * 10.0f, rotation, PlayerRef.None);
				asteroid.InitState(force);
			}
		}
	}
}