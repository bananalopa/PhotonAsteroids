using UnityEngine;
using Fusion;

namespace Asteroids.SharedSimple
{
    // Holds the player's information and ensures it is replicated to all clients.
    public class PlayerDataNetworked : NetworkBehaviour
    {
        // Global static setting
        private const int STARTING_LIVES = 3;

        // Local Runtime references
        private PlayerOverviewPanel _overviewPanel = null;

        private ChangeDetector _changeDetector;

        // Game Session SPECIFIC Settings are used in the UI.
        // The method passed to the OnChanged attribute is called everytime the [Networked] parameter is changed.
        [HideInInspector]
        [Networked]
        public NetworkString<_16> NickName { get; private set; }

        [HideInInspector]
        [Networked]
        public int Lives { get; private set; }

        [HideInInspector]
        [Networked]
        public int Score { get; private set; }

        public override void Spawned()
        {

            // --- StateAuthority
            // Initialized game specific settings
            if (Object.HasStateAuthority)
            {
                Lives = STARTING_LIVES;
                Score = 0;
                NickName = LocalPlayerData.NickName;
            }

            // --- All Clients
            // Set the local runtime references.
            
            FindObjectOfType<GameController>().TrackNewPlayer(this);
            _overviewPanel = FindObjectOfType<PlayerOverviewPanel>();
            // Add an entry to the local Overview panel with the information of this spaceship
            _overviewPanel.AddEntry(Object.InputAuthority, this);
            
            // Refresh panel visuals in Spawned to set to initial values.
            _overviewPanel.UpdateEntry(this);

            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }
        
        // Remove the entry in the local Overview panel for this spaceship
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _overviewPanel.RemoveEntry(this);
        }
        
        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
            {
                _overviewPanel.UpdateEntry(this);
                break;
            }
        }

        // Increase the score by X amount of points
        public void AddToScore(int points)
        {
            Score += points;
        }

        // Decrease the current Lives by 1
        public void SubtractLife()
        {
            Lives--;
        }

        // RPC used to send player information to the Host
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
        private void RpcSetNickName(string nickName)
        {
            if (string.IsNullOrEmpty(nickName)) return;
            NickName = nickName;
        }
    }
}