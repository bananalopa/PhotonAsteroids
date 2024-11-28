using Fusion;
using UnityEngine;

namespace Asteroids.SharedSimple
{
	// Manage spawning of new large asteroid. Only executes on the master client.
	[RequireComponent(typeof(GameController))]
	public class AsteroidSpawner : NetworkBehaviour, IStateAuthorityChanged
	{
		// The Network Object prefabs for small and big asteroids.
		// Using NetworkPrefabRef restricts the parameters to Prefabs which carry a NetworkObject component. 
		[Tooltip("Prefab for small asteroids")]
		[SerializeField] private NetworkPrefabRef _smallAsteroid = NetworkPrefabRef.Empty;
		[Tooltip("Prefab for automatically spawned large asteroids")]
		[SerializeField] private AsteroidBehaviour _bigAsteroid;

		[Tooltip("Minimum delay between automatic spawning of asteroids")]
		[SerializeField] private float _minSpawnDelay = 5.0f;

		[Tooltip("Maximum delay between automatic spawning of asteroids")]
		[SerializeField] private float _maxSpawnDelay = 10.0f;
		
		// The minimum and maximum amount of small asteroids a big asteroids spawns when it gets destroyed.
		[SerializeField] private int _minAsteroidSplinters = 3;
		[SerializeField] private int _maxAsteroidSplinters = 6;

		// The TickTimer controls the time lapse between spawns.
		private TickTimer _spawnDelay;

		private GameController _gameController;

		public void StartAsteroidSpawner()
		{
			if (Object.HasStateAuthority == false) return;

			// Triggers the delay until the first spawn.
			_gameController = GetComponent<GameController>();
			SetSpawnDelay();
		}

		public override void FixedUpdateNetwork()
		{
			SpawnAsteroids();
		}
		
		public void SpawnAsteroids()
		{
			if (_spawnDelay.Expired(Runner) == false) return;

			Vector2 direction = Random.insideUnitCircle;
			Vector3 position = Vector3.zero;

			if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
			{
				// Make it appear on the left/right side
				position = new Vector3(Mathf.Sign(direction.x) * _gameController.ScreenBoundaryX, 0, direction.y * _gameController.ScreenBoundaryY);
			}
			else
			{
				// Make it appear on the top/bottom
				position = new Vector3(direction.x * _gameController.ScreenBoundaryX, 0, Mathf.Sign(direction.y) * _gameController.ScreenBoundaryY);
			}

			// Offset slightly so we are not out of screen at creation time (as it would destroy the asteroid right away)
			position -= position.normalized * 0.1f;

			Quaternion rotation = Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f));
			AsteroidBehaviour asteroid = _gameController.Runner.Spawn(_bigAsteroid, position, rotation, PlayerRef.None);
			asteroid.InitState(-asteroid.transform.position.normalized * 1000.0f);

			SetSpawnDelay();
		}
		
		private void SetSpawnDelay()
		{
			// Chose a random amount of time until the next spawn.
			var time = Random.Range(_minSpawnDelay, _maxSpawnDelay);
			_spawnDelay = TickTimer.CreateFromSeconds(Runner, time);
		}

		public void StateAuthorityChanged()
		{
			StartAsteroidSpawner();
		}
	}
}