#nullable enable

using System;

namespace Ur.Data {

    class GameObject {
        public Guid Id { get; set; }
    }

    record TilePosition(int Column, int Row);

    class Game : GameObject {
        public Player[] Players { get; } = new Player[2] {
            new Player(),
            new Player { PieceColor = "black" }
        };
        readonly GamePiece[] pieces;
        public GamePiece[] Pieces => pieces;
        public int ActivePlayerIndex { get; set; }
        public Game()
        {
            pieces = new GamePiece[7*Players.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new GamePiece(Players[i / 7], i % 7);
            }
        }
    }

    class Player : GameObject {
        public string PieceColor { get; set; } = "white";
    }

    class GamePiece : GameObject {
        public Player Player { get; private set; }
        public int PieceIndex { get; }

        public string Color => Player.PieceColor;
        public TilePosition Position { get; set; }
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