using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit.Model
{
    public class Tetris
    {
        private readonly System.Random random = new System.Random();
        private readonly Color[] colors;
        private readonly Texture2D texture;
        private readonly int blockSize;
        private TetrisBlock currentBlock;
        private readonly List<TetrisBlock> blocks;
        private int currentTime;
        private const int TimeOneTick = 50;

        private int GridWidth { get { return texture.width / blockSize; } }
        private int GridHeight { get { return texture.height / blockSize; } }

        abstract class TetrisBlock
        {
            public int posX;
            public int posY;
            private int rotation;
            protected abstract Color BlockColor { get; }
            private int BlockCount = 4;
            protected abstract int BlockPosX(int blockIndex);
            protected abstract int BlockPosY(int blockIndex);
            protected abstract TetrisBlock Clone();

            private void Rotate(ref int x, ref int y)
            {
                switch (rotation)
                {
                    case 0:
                        break;
                    case 1:
                        var tmp = x;
                        x = y;
                        y = -tmp;
                        break;
                    case 2:
                        x = -x;
                        y = -y;
                        break;
                    case 3:
                        x = -x;
                        y = -y;
                        goto case 1;
                }
            }

            private void GetBlockPos(out int x, out int y, int blockIndex)
            {
                x = BlockPosX(blockIndex);
                y = BlockPosY(blockIndex);
                Rotate(ref x, ref y);
                x += posX;
                y += posY;
            }

            public void Render(Tetris tetris)
            {
                var color = BlockColor;
                var edgeColor = color * 0.5f;
                for (int i = 0; i < BlockCount; i++)
                {
                    int x, y;
                    GetBlockPos(out x, out y, i);
                    var starty = y * tetris.blockSize;
                    var endy = (y + 1) * tetris.blockSize;
                    var startx = x * tetris.blockSize;
                    var endx = (x + 1) * tetris.blockSize;
                    for (int py = starty; py < endy; py++)
                    {
                        bool colorY = py == starty || py == endy - 1;
                        for (int px = startx; px < endx; px++)
                        {
                            bool colorDark = colorY || px == startx || px == endx - 1;
                            var actualColor = colorDark ? edgeColor : color;
                            tetris.colors[tetris.texture.width * py + px] = actualColor;
                        }
                    }
                }
            }

            public void Die(Tetris tetris)
            {
                tetris.blocks.Remove(this);
                for (int i = 0; i < BlockCount; i++)
                {
                    int x, y;
                    GetBlockPos(out x, out y, i);
                    var clone = Clone();
                    int cloneZeroX, cloneZeroY;
                    clone.GetBlockPos(out cloneZeroX, out cloneZeroY, 0);
                    clone.posX = x - cloneZeroX;
                    clone.posY = y - cloneZeroY;
                    clone.BlockCount = 1; // woo hacks!
                    tetris.blocks.Add(clone);
                }
            }

            public bool HasBlock(int xval, int yval)
            {
                for (int i = 0; i < BlockCount; i++)
                {
                    int x, y;
                    GetBlockPos(out x, out y, i);
                    if (x == xval && y == yval)
                        return true;
                }
                return false;
            }

            public bool HasYval(int yval)
            {
                for (int i = 0; i < BlockCount; i++)
                {
                    int x, y;
                    GetBlockPos(out x, out y, i);
                    if (y == yval)
                        return true;
                }
                return false;
            }

            internal void ShiftDownIfAbove(int line)
            {
                int x, y;
                GetBlockPos(out x, out y, 0);
                if (y > line)
                {
                    posY--;
                }
            }

            private bool IsCurrentlyLegal(Tetris tetris)
            {
                for (int i = 0; i < BlockCount; i++)
                {
                    int myX, myY;
                    GetBlockPos(out myX, out myY, i);
                    if (myX < 0 || myX >= tetris.GridWidth || myY < 0 || myY >= tetris.GridHeight)
                    {
                        return false;
                    }
                    foreach (var other in tetris.blocks)
                    {
                        if (this == other)
                            continue;
                        for (int j = 0; j < other.BlockCount; j++)
                        {
                            int theirX, theirY;
                            other.GetBlockPos(out theirX, out theirY, j);
                            if (myX == theirX && myY == theirY)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

            public bool TryMoveDelta(Tetris tetris, int dX, int dY)
            {
                posX += dX;
                posY += dY;
                if (IsCurrentlyLegal(tetris))
                    return true;
                posX -= dX;
                posY -= dY;
                return false;
            }

            public bool TryRotateDelta(Tetris tetris, int delta)
            {
                var oldX = posX;
                for (int i = 0; i < 3; i++)
                {
                    var oldRotation = rotation;
                    rotation += delta;
                    rotation %= 4;
                    if (rotation < 0)
                        rotation += 4;
                    posX = oldX + delta * i;
                    if (IsCurrentlyLegal(tetris))
                        return true;
                    rotation = oldRotation;
                }
                posX = oldX;
                return false;
            }
        }

        class ShapeI : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeI(); }

            protected override Color BlockColor { get { return Color.gray; } }

            protected override int BlockPosX(int blockIndex)
            {
                return 0;
            }

            protected override int BlockPosY(int blockIndex)
            {
                return blockIndex - 1;
            }
        }

        class ShapeJ : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeJ(); }

            protected override Color BlockColor { get { return Color.green; } }

            protected override int BlockPosX(int blockIndex)
            {
                if (blockIndex == 3)
                    return -1;
                return 0;
            }

            protected override int BlockPosY(int blockIndex)
            {
                if (blockIndex == 3)
                    return 1;
                return blockIndex - 1;
            }
        }

        class ShapeL : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeL(); }

            protected override Color BlockColor { get { return Color.blue; } }

            protected override int BlockPosX(int blockIndex)
            {
                if (blockIndex == 3)
                    return 1;
                return 0;
            }

            protected override int BlockPosY(int blockIndex)
            {
                if (blockIndex == 3)
                    return 1;
                return blockIndex - 1;
            }
        }

        class ShapeO : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeO(); }

            protected override Color BlockColor { get { return Color.yellow; } }

            protected override int BlockPosX(int blockIndex)
            {
                return blockIndex % 2;
            }

            protected override int BlockPosY(int blockIndex)
            {
                return blockIndex / 2;
            }
        }

        class ShapeZ : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeZ(); }

            protected override Color BlockColor { get { return Color.cyan; } }

            protected override int BlockPosX(int blockIndex)
            {
                if (blockIndex < 2)
                    return blockIndex - 1;
                return blockIndex - 2;
            }

            protected override int BlockPosY(int blockIndex)
            {
                return blockIndex / 2;
            }
        }

        class ShapeS : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeS(); }

            protected override Color BlockColor { get { return Color.magenta; } }

            protected override int BlockPosX(int blockIndex)
            {
                if (blockIndex < 2)
                    return blockIndex;
                return blockIndex - 3;
            }

            protected override int BlockPosY(int blockIndex)
            {
                return blockIndex / 2;
            }
        }

        class ShapeT : TetrisBlock
        {
            protected override TetrisBlock Clone() { return new ShapeT(); }

            protected override Color BlockColor { get { return Color.red; } }

            protected override int BlockPosX(int blockIndex)
            {
                if (blockIndex == 3)
                    return 0;
                return blockIndex - 1;
            }

            protected override int BlockPosY(int blockIndex)
            {
                return blockIndex / 3;
            }
        }

        private void Spawn()
        {
            switch (random.Next(7))
            {
                case 0:
                    currentBlock = new ShapeI();
                    break;
                case 1:
                    currentBlock = new ShapeJ();
                    break;
                case 2:
                    currentBlock = new ShapeL();
                    break;
                case 3:
                    currentBlock = new ShapeO();
                    break;
                case 4:
                    currentBlock = new ShapeZ();
                    break;
                case 5:
                    currentBlock = new ShapeS();
                    break;
                case 6:
                    currentBlock = new ShapeT();
                    break;
            }
            currentBlock.posX = GridWidth / 2;
            currentBlock.posY = GridHeight;
            var tries = 0;
            while (!currentBlock.TryMoveDelta(this, 0, -1))
            {
                currentBlock.posY -= 1;
                if (tries++ > 6)
                {
                    blocks.Clear(); // reset everything (game over)
                    Spawn();
                }
            }
            blocks.Add(currentBlock);
        }

        private void RotateBlock(int rotation)
        {
            currentBlock.TryRotateDelta(this, rotation);
        }

        private void MoveBlockSide(int side)
        {
            currentBlock.TryMoveDelta(this, side, 0);
        }

        private void CheckDeadRows()
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var allPresent = true;
                for (int x = 0; x < GridWidth; x++)
                {
                    var hasThisX = false;
                    foreach (var block in blocks)
                    {
                        if (block.HasBlock(x, y))
                        {
                            hasThisX = true;
                            break;
                        }
                    }
                    if (!hasThisX)
                    {
                        allPresent = false;
                        break;
                    }
                }
                if (allPresent)
                {
                    blocks.RemoveAll(b => b.HasYval(y));
                    foreach (var block in blocks)
                    {
                        block.ShiftDownIfAbove(y);
                    }
                    // score += 1! Yay!
                }
            }
        }

        private void MoveBlockDown(bool dropBlock)
        {
            while (currentBlock.TryMoveDelta(this, 0, -1))
            {
                if (!dropBlock)
                {
                    return;
                }
            }
            currentBlock.Die(this);
            currentBlock = null;
            CheckDeadRows();
            Spawn();
        }

        public Tetris(Texture2D renderTarget, int blockSize)
        {
            this.texture = renderTarget;
            this.blockSize = blockSize;
            this.blocks = new List<TetrisBlock>();
            colors = new Color[renderTarget.width * renderTarget.height];
            Spawn();
        }

        public void RunUpdate(bool keyLeft, bool keyRight, bool keyRotLeft, bool keyRotRight, bool keyDown, bool keyDrop)
        {
            currentTime++;
            if (currentTime >= TimeOneTick)
            {
                currentTime = 0;
                MoveBlockDown(false);
            }
            if (keyLeft)
            {
                MoveBlockSide(-1);
            }
            if (keyRight)
            {
                MoveBlockSide(1);
            }
            if (keyRotLeft)
            {
                RotateBlock(-1);
            }
            if (keyRotRight)
            {
                RotateBlock(1);
            }
            if (keyDown)
            {
                currentTime = 0;
                MoveBlockDown(false);
            }
            if (keyDrop)
            {
                currentTime = 0;
                MoveBlockDown(true);
            }
        }

        public Texture2D Render()
        {
            Array.Clear(colors, 0, colors.Length);
            foreach (var block in blocks)
            {
                block.Render(this);
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
