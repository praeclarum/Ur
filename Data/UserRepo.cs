#nullable enable

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace Ur.Data {
    public class UserRepo {
        readonly ConcurrentDictionary<string, User> users = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public User NewUser(string name) {
            var user = new User { Name = name };
            users.TryAdd(user.Id, user);
            OnPropertyChanged(nameof(AllUsers));
            return user;
        }

        public User[] AllUsers => users.Values.ToArray();

        public User? FindUser(string id) {
            return users.TryGetValue(id, out var game) ? game : null;
        }
    }
}
