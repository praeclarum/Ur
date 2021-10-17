#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ComponentModel;

namespace Ur.Data {
    class GameRepo : INotifyPropertyChanged {
        readonly ConcurrentDictionary<string, Game> games = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Game NewGame() {
            var game = new Game();
            games.TryAdd(game.Id, game);
            OnPropertyChanged(nameof(AllGames));
            return game;
        }

        public Game[] AllGames => games.Values.ToArray();

        public Game? FindGame(string id) {
            return games.TryGetValue(id, out var game) ? game : null;
        }
    }
}
