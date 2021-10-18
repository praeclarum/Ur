#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace Ur.Data
{

    public class GameObject : INotifyPropertyChanged {
        public string Id { get; } = Guid.NewGuid().ToString();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Game : GameObject
    {
        public string Title { get; set; } = "";
        public Player[] Players { get; } = new Player[2] {
            new Player(0),
            new Player(1) { PieceColor = "black" }
        };
        public bool IsActive => !IsPendingPlayer;
        public bool IsPendingPlayer => Players.Any(p => !p.HasUser);
        readonly GamePiece[] pieces;
        public GamePiece[] Pieces => pieces;
        public int ActivePlayerIndex { get; private set; }
        public Player ActivePlayer => Players[ActivePlayerIndex];
        public int RolledPlacesToMove
        {
            get => rolledPlacesToMove;
            private set
            {
                rolledPlacesToMove = value;
                OnPropertyChanged(nameof(RolledPlacesToMove));
            }
        }
        readonly Random random = new Random();
        public DateTimeOffset StartTime { get; } = DateTimeOffset.Now;
        public DateTimeOffset ActiveTime
        {
            get => activeTime;
            private set
            {
                activeTime = value;
                OnPropertyChanged(nameof(ActiveTime));
            }
        }
        public Game()
        {
            pieces = new GamePiece[7 * Players.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new GamePiece(Players[i / 7], i % 7);
            }
            Roll();
        }

        string status = "";
        private int rolledPlacesToMove = 0;
        private DateTimeOffset activeTime = DateTimeOffset.Now;

        public DateTimeOffset? EndTime { get; private set; }
        public bool IsCompleted => EndTime.HasValue;

        void GameOver(Player winner)
        {
            Status = $"{winner.InitialUserName} won!";
            EndTime = DateTimeOffset.Now;
        }

        public string Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public async Task MovePieceAsync(GamePiece piece)
        {
            ActiveTime = DateTimeOffset.Now;

            var (movement, newPosition) = GetPieceMovement(piece);

            switch (movement) {
                case Movement.NoNotTurn:
                    piece.Player.GameMessage = "Not your turn!";
                    return;
                case Movement.NoCantMove:
                    piece.Player.GameMessage = "You can't move that piece";
                    return;
                case Movement.NoOccupied:
                    piece.Player.GameMessage = "Destination is occupied";
                    return;
                case Movement.NoRosette:
                    piece.Player.GameMessage = "Rosettes are protected";
                    return;
                case Movement.NoRolling:
                    piece.Player.GameMessage = "Wait for roll to finish";
                    return;
                case Movement.OKScore:
                    piece.Player.GameMessage = "Score!";
                    piece.Position = newPosition;
                    piece.IsOut = true;
                    piece.Player.Score++;
                    if (piece.Player.Score == 7)
                    {
                        piece.Player.GameMessage = "You win!";
                        GameOver(piece.Player);
                    }
                    else {
                        ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
                        await NextTurnAsync ();
                    }
                    return;
            }

            var newProgress = piece.Position.GetProgress() + RolledPlacesToMove;

            if (movement == Movement.OKCapture) {
                var capturedPiece = FindPieceAt(newPosition);
                if (capturedPiece != null)
                {
                    piece.Player.GameMessage = $"Captured {capturedPiece.Color}!";
                    capturedPiece.Position = new TilePosition(capturedPiece.Player.OutsideColumn,
                                                                1 + capturedPiece.PieceIndex);
                }
            }            
            else {
                piece.Player.GameMessage = "";
            }
            piece.Position = newPosition;
            if (piece.Position.IsRosette()) {
                await NextTurnAsync($"You get another turn!");
            }
            else {
                ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
                await NextTurnAsync();
            }
        }

        public bool CanMoveTo(TilePosition position, Player player)
        {
            var otherPiece = FindPieceAt(position);
            return otherPiece switch
            {
                null => true,
                GamePiece p => p.Player != player
            };
        }

        enum Movement {
            NoNotTurn = 0,
            NoCantMove = 1,
            NoOccupied = 2,
            NoRosette = 3,
            NoRolling = 4,
            OK = 100,
            OKScore = 101,
            OKCapture = 102,
        }

        (Movement, TilePosition) GetPieceMovement(GamePiece piece)
        {
            if (RolledPlacesToMove < 0)
                return (Movement.NoRolling, piece.Position);

            if (piece.Player != Players[ActivePlayerIndex])
            {
                return (Movement.NoNotTurn, piece.Position);
            }

            var newProgress = piece.Position.GetProgress() + RolledPlacesToMove;
            var newPosition = TilePosition.FromProgress(newProgress, piece.Player) ?? piece.Position;

            if (newProgress == 14) {
                return (Movement.OKScore, newPosition);
            }
            
            if (newPosition == piece.Position)
            {
                return (Movement.NoCantMove, newPosition);
            }

            if (!CanMoveTo(newPosition, piece.Player))
            {
                return (Movement.NoOccupied, newPosition);
            }

            var capturedPiece = FindPieceAt(newPosition);
            if (capturedPiece != null && capturedPiece.Player != piece.Player)
            {
                if (newPosition.IsRosette()) {
                    return (Movement.NoRosette, newPosition);
                }
                else {
                    return (Movement.OKCapture, newPosition);
                }
            }

            return (Movement.OK, newPosition);
        }

        bool CanMovePiece(GamePiece piece)
        {
            return GetPieceMovement(piece).Item1 switch
            {
                Movement.OK => true,
                Movement.OKCapture => true,
                Movement.OKScore => true,
                _ => false
            };
        }

        bool CanMovePiece()
        {
            var player = ActivePlayer;
            foreach (var piece in Pieces)
            {
                if (piece.Player != player || piece.IsOut)
                    continue;
                if (CanMovePiece(piece)) {
                    return true;                
                }
            }
            return false;
        }

        GamePiece? FindPieceAt(TilePosition position)
        {
            return pieces.FirstOrDefault(p => p.Position == position);
        }

        async Task NextTurnAsync(string message = "")
        {
            ActivePlayer.GameMessage = message;
            await RollAsync();

            while (RolledPlacesToMove == 0 || !CanMovePiece())
            {
                if (RolledPlacesToMove == 0) {
                    ActivePlayer.GameMessage = "Rolled 0, no move.";
                }
                else {
                    ActivePlayer.GameMessage = "No pieces can move.";
                }
                await Task.Delay(2000);
                ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
                ActivePlayer.GameMessage = "";
                await RollAsync();
            }
        }

        async Task RollAsync() {
            RolledPlacesToMove = -1;
            await Task.Delay(250 + random.Next(1000));
            Roll();
        }

        void Roll() {
            var d0 = random.Next(2);
            var d1 = random.Next(2);
            var d2 = random.Next(2);
            var d3 = random.Next(2);
            RolledPlacesToMove = d0 + d1 + d2 + d3;
        }
    }

    public class GamePiece : GameObject {
        public Player Player { get; private set; }
        public int PieceIndex { get; }

        public string Color => Player.PieceColor;
        public TilePosition Position { get; set; }
        public bool IsOut { get; set; }
        public GamePiece(Player player, int pieceIndex)
        {
            Player = player;
            PieceIndex = pieceIndex;
            Position = new TilePosition (
                Color == "white" ? 0 : 4,
                pieceIndex + 1);
        }
    }

    
}