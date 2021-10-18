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
        bool isAI = false;
        public bool IsAI
        {
            get => isAI;
            set
            {
                isAI = value;
                InitialUserName = "AI";
            }
        }
        public string? GetUserName(UserRepo? repo) => 
            isAI ? "AI" : (string.IsNullOrEmpty(UserId) ? null : (repo?.FindUser(UserId)?.Name ?? InitialUserName));
        public bool HasUser => isAI || !string.IsNullOrEmpty(UserId);
        public Player(int index)
        {
            Index = index;
        }
    }

    
}