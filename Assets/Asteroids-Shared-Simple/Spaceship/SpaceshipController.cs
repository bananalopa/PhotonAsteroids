using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Asteroids.SharedSimple
{
    // This class controls the lifecycle of the spaceship.
    // It derives from FusionPlayer which is just a helper NetworkBehaviour that provides some basic
    // player management features as well as the tick aligned event system used by asteroids.
    public class SpaceshipController : NetworkBehaviour
    {
        // Game Session AGNOSTIC Settings
        [SerializeField] private float _respawnDelay = 2.0f;
        [SerializeField] private float _spaceshipDamageRadius = 3.0f;
        [SerializeField] private float _rotationSpeed = 12.0f;
        [SerializeField] private float _acceleration = 5000.0f;
        [SerializeField] private float _maxSpeed = 1000.0f;
        [SerializeField] private float _delayBetweenShots = 0.2f;
        [SerializeField] private BulletBehaviour _bullet;
        [SerializeField] private LayerMask _asteroidCollisionLayer;
        [SerializeField] private MeshRenderer _spaceshipModel = null;
        [SerializeField] private ParticleSystem _destructionVFX = null;
        [SerializeField] private ParticleSystem _engineTrailVFX = null;

        // Game Session SPECIFIC Settings
        [Networked] private NetworkBool IsAlive { get; set; }
        [Networked] private TickTimer RespawnTimer { get; set; }
        [Networked] private float Acceleration { get; set; }

        // Local Runtime references
        private Rigidbody _rigidbody;
        private Collider[] _hits = new Collider[1];
        private ChangeDetector _changeDetector;
        private PlayerDataNetworked _playerDataNetworked = null;
        private TickTimer _shootCooldown;
        private NetworkButtons ButtonsPrevious { get; set; }

        
        public bool AcceptInput => IsAlive && Object.IsValid;

        public override void Spawned()
        {
            _playerDataNetworked = GetComponent<PlayerDataNetworked>();

            // Colors the ship in the color assigned to the PlayerRef's index
            var playerRef = Object.InputAuthority;
            _spaceshipModel.material.color = GetColor(playerRef.PlayerId);

            // Grab a change detector for this NB so we can detect when a life is lost and play an appropriate effect
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            // We're controlling the ship using forces, so grab the rigidbody
            _rigidbody = GetComponent<Rigidbody>();

            if (HasStateAuthority)
            {
                IsAlive = true;
            }
        }

        public override void Render()
        {
            // Adjust the engine effect based on acceleration
            ParticleSystem.EmissionModule e = _engineTrailVFX.emission;
            e.rateOverTimeMultiplier = Mathf.Abs(Acceleration);
            var main = _engineTrailVFX.main;
            main.startSpeedMultiplier = -0.2f * Acceleration;

            // Check if there were any changes to our network state and handle changes in our alive state
            foreach (var change in _changeDetector.DetectChanges(this, out var prev, out var current))
            {
                switch (change)
                {
                    case nameof(IsAlive):
                    {
                        // Get a property reader and read the previous and current values of the changed property
                        (var wasAlive, var isAlive) = GetPropertyReader<NetworkBool>(change).Read(prev, current);
                        ToggleVisuals(wasAlive, isAlive);
                        break;
                    }
                }
            }
        }

        private void ToggleVisuals(bool wasAlive, bool isAlive)
        {
            // Check if the spaceship was just brought to life
            if (isAlive && !wasAlive)
            {
                _spaceshipModel.enabled = true;
                _engineTrailVFX.Play();
                _destructionVFX.Stop();
            }
            // or whether it just got destroyed.
            else if (wasAlive && !isAlive)
            {
                _spaceshipModel.enabled = false;
                _engineTrailVFX.Stop();
                _destructionVFX.Play();
            }
        }

        public static Color GetColor(int player)
        {
            switch (player % 8)
            {
                case 0: return Color.red;
                case 1: return Color.green;
                case 2: return Color.blue;
                case 3: return Color.yellow;
                case 4: return Color.cyan;
                case 5: return Color.grey;
                case 6: return Color.magenta;
                case 7: return Color.white;
            }

            return Color.black;
        }

        public override void FixedUpdateNetwork()
        {
            var gamestate = GameController.Singleton;

            if (!gamestate.GameIsRunning)
                return;

            // Checks if the spaceship is ready to be respawned.
            if (RespawnTimer.Expired(Runner))
            {
                IsAlive = true;
                RespawnTimer = default;
            }

            // Checks if the spaceship got hit by an asteroid
            if (IsAlive && HasHitAsteroid())
            {
                ShipWasHit();
            }

            // Handle input if we have StateAuthority over the object (GetInput returns false otherwise)
            if (AcceptInput && GetInput<SpaceshipInput>(out var input))
            {
                Move(input);
                Fire(input);
            }

            // Keep spaceship on screen
            CheckExitScreen(gamestate);
        }

        private bool HasHitAsteroid()
        {
            var count = Runner.GetPhysicsScene().OverlapSphere(_rigidbody.position, _spaceshipDamageRadius, _hits,
                _asteroidCollisionLayer.value, QueryTriggerInteraction.UseGlobal);

            if (count <= 0)
                return false;

            var asteroidBehaviour = _hits[0].GetComponent<AsteroidBehaviour>();
            
            return asteroidBehaviour.OnAsteroidHit();
        }

        // Toggle the IsAlive boolean if the spaceship was hit and check whether the player has any lives left.
        // If they do, then the RespawnTimer is activated.
        private void ShipWasHit()
        {
            if (!HasStateAuthority) return;
            
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            
            IsAlive = false;

            if (_playerDataNetworked.Lives > 1)
                RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
            else
                RespawnTimer = default;

            _playerDataNetworked.SubtractLife();
        }

        // Moves the spaceship RB using the input for the client with InputAuthority over the object
        private void Move(SpaceshipInput input)
        {
            Transform xform = transform;
            _rigidbody.AddRelativeTorque(
                Mathf.Clamp(input.HorizontalInput, -1, 1) * _rotationSpeed * Runner.DeltaTime * xform.up,
                ForceMode.VelocityChange);

            Acceleration = Mathf.Clamp(input.VerticalInput, -1, 1) * _acceleration * Runner.DeltaTime;
            Vector3 force = xform.forward * Acceleration;
            _rigidbody.AddForce(force);

            if (_rigidbody.linearVelocity.magnitude > _maxSpeed)
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maxSpeed;
        }

        // Moves the ship to the opposite side of the screen if it exits the screen boundaries.
        private void CheckExitScreen(GameController ctrl)
        {
            var position = _rigidbody.position;

            if (Mathf.Abs(position.x) < ctrl.ScreenBoundaryX && Mathf.Abs(position.z) < ctrl.ScreenBoundaryY) return;

            if (Mathf.Abs(position.x) > ctrl.ScreenBoundaryX)
                position = new Vector3(-Mathf.Sign(position.x) * ctrl.ScreenBoundaryX, 0, position.z);

            if (Mathf.Abs(position.z) > ctrl.ScreenBoundaryY)
                position = new Vector3(position.x, 0, -Mathf.Sign(position.z) * ctrl.ScreenBoundaryY);

            position -= position.normalized *
                        0.1f; // offset a little bit to avoid looping back & forth between the 2 edges 

            GetComponent<NetworkRigidbody3D>().Teleport(position);
        }

        // Checks the Buttons in the input struct against their previous state to check
        // if the fire button was just pressed.
        private void Fire(SpaceshipInput input)
        {
            if (input.Buttons.WasPressed(ButtonsPrevious, SpaceshipButtons.Fire))
                SpawnBullet();

            ButtonsPrevious = input.Buttons;
        }

        // Spawns a bullet which will be travelling in the direction the spaceship is facing
        private void SpawnBullet()
        {
            if (_shootCooldown.ExpiredOrNotRunning(Runner) == false) return;

            Runner.Spawn(_bullet, _rigidbody.position, _rigidbody.rotation, Object.InputAuthority);

            _shootCooldown = TickTimer.CreateFromSeconds(Runner, _delayBetweenShots);
        }
    }
}