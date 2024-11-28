using Fusion;
using UnityEngine;

namespace Asteroids.SharedSimple
{
    // The SpaceshipSpawner, just like the AsteroidSpawner, only executes on the Host.
    // Therefore none of its parameters need to be [Networked].
    public class SpaceshipSpawner : NetworkBehaviour
    {
        [Networked] private bool _gameIsReady { get; set; } = false;
        
        // References to the NetworkObject prefab to be used for the players' spaceships.
        [SerializeField] private NetworkPrefabRef _spaceshipNetworkPrefab = NetworkPrefabRef.Empty;
        
        private GameController _gameStateController = null;

        private SpawnPoint[] _spawnPoints = null;

        public override void Spawned()
        {
            // Collect all spawn points in the scene.
            _spawnPoints = FindObjectsOfType<SpawnPoint>();

            // When the SpaceshipSpawner gets spawned on a late joiner, spawn a spaceship for them
            if (_gameIsReady) 
            {
                SpawnSpaceship(Runner.LocalPlayer);
            }
        }

        // The spawner is started when the GameController switches to GameState.Running.
        public void StartSpaceshipSpawner(GameController gameController)
        {
            _gameIsReady = true;
            _gameStateController = gameController;
            RpcInitialSpaceshipSpawn();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcInitialSpaceshipSpawn()
        {
            SpawnSpaceship(Runner.LocalPlayer);
        }

        // Spawns a spaceship for a player.
        // The spawn point is chosen in the _spawnPoints array using the implicit playerRef to int conversion 
        private void SpawnSpaceship(PlayerRef player)
        {
            // Modulo is used in case there are more players than spawn points.
            int index = player.PlayerId % _spawnPoints.Length;
            var spawnPosition = _spawnPoints[index].transform.position;

            var playerObject = Runner.Spawn(_spaceshipNetworkPrefab, spawnPosition, Quaternion.identity, player);
            // Set Player Object to facilitate access across systems.
            Runner.SetPlayerObject(player, playerObject);
        }
    }
}