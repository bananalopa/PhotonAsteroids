using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

namespace Asteroids.SharedSimple
{
    // A purely utilitarian class which manages the display of player information (Nickname, Lives and Score)
    public class PlayerOverviewPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerOverviewEntryPrefab = null;

        private Dictionary<PlayerDataNetworked, TextMeshProUGUI> _playerListEntries =
            new Dictionary<PlayerDataNetworked, TextMeshProUGUI>();

        public void Clear()
        {
            foreach (var tmp in _playerListEntries.Values)
            {
                Destroy(tmp);
            }

            _playerListEntries.Clear();
        }

        public void AddEntry(PlayerRef playerRef, PlayerDataNetworked playerDataNetworked)
        {
            if (_playerListEntries.ContainsKey(playerDataNetworked)) return;
            if (playerDataNetworked == null) return;

            var entry = Instantiate(_playerOverviewEntryPrefab, this.transform);
            entry.transform.localScale = Vector3.one;
            entry.color = SpaceshipController.GetColor(playerRef.PlayerId);

            _playerListEntries.Add(playerDataNetworked, entry);

            UpdateEntry(playerDataNetworked);
        }

        public void UpdateEntry(PlayerDataNetworked playerData)
        {
            if (_playerListEntries.TryGetValue(playerData, out TextMeshProUGUI entry))
            {
                entry.text = $"{playerData.NickName}\nScore: {playerData.Score}\nLives: {playerData.Lives}";
            }
        }


        public void RemoveEntry(PlayerDataNetworked playerData)
        {
            if (_playerListEntries.TryGetValue(playerData, out var entry) == false) return;

            if (entry != null)
            {
                Destroy(entry.gameObject);
            }

            _playerListEntries.Remove(playerData);
        }
    }
}