using System;
using DockTest.ExternalDeps.Classes.ElementProps;
using DockTest.Source.Properties.Vector;

namespace DockTest.Source.Properties
{
        public class Transform : ElementProperty<Transform>
        {
            private Position _position = new();

            private Size _size = new();

            
            public Transform()
            {
                Position = new();
                Size = new();
            }

            public bool[] SizeLock = {true};
            
            public Size Size
            {
                get => _size;
                set
                {
                    if (_size.Equals(value)) return;
                    _size = new Size(value.Width, value.Height);
                    _size.PropertyChanged += (a, b) =>
                    {
                        if (SizeLock[0])
                        {
                            OnResize?.Invoke(this, _size);
                        }
                    };
                    if (SizeLock[0])
                    {
                        OnResize?.Invoke(this, _size);
                    }
                }
            }
            
            public void SetSize(Size size)
            {
                lock (SizeLock)
                {
                    SizeLock[0] = false;

                    Size = size;
                    
                    SizeLock[0] = true;
                }
            }

            public Action<Transform, Size> OnResize
            {
                get => GetPropertyActionCall<Size>().Invoke;
                set => GetPropertyActionCall<Size>().Action += value;
            }

            public bool[] PositionLock = {true};
            
            public Position Position
            {
                get => _position;
                set
                {
                    if (_position.Equals(value)) return;
                    _position = new Position(value.X, value.Y);
                    _position.PropertyChanged += (a, b) =>
                    {
                        if (PositionLock[0])
                        {
                            OnMove?.Invoke(this, _position);
                        }
                    };
                    if (PositionLock[0])
                    {
                        OnMove?.Invoke(this, _position);
                    }
                }
            }

            public void SetPosition(Position pos)
            {
                lock (PositionLock)
                {
                    PositionLock[0] = false;

                    Position = pos;
                    
                    PositionLock[0] = true;
                }
            }

            public Action<Transform, Position> OnMove
            {
                get => GetPropertyActionCall<Position>().Invoke;
                set => GetPropertyActionCall<Position>().Action = value;
            }

            public void SetPositionSize(int x, int y, int w, int h)
            {
                SetPositionSize(new Position(x, y), new Size(w, h));
            }

            public void SetPositionSize(Position position, Size size)
            {
                _position = position;
                Size = size;
            }
    }
}