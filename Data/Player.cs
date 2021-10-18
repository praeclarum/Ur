#nullable enable


namespace Ur.Data
{
    public class Player : GameObject {
        public string PieceColor { get; set; } = "white";
        public int Index { get; }
        public int OutsideColumn => Index == 0 ? 0 : 4;
        public string? UserId {get; set;} = "";
        public string? InitialUserName {get; set;} = "";
        public string GameMessage { get; set; } = "";
        public int Score { get; set; } = 0;
        public string? GetUserName(UserRepo repo) => 
            string.IsNullOrEmpty(UserId) ? null : repo.FindUser(UserId)?.Name;
        public bool HasUser => !string.IsNullOrEmpty(UserId);
        public Player(int index)
        {
            Index = index;
        }
    }

    
}