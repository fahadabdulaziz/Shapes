﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace SixLabors.Shapes.Tests
{
    using SixLabors.Primitives;
    using System.Numerics;

    [Serializable]
    public class TestPoint : IXunitSerializable
    {
        public TestPoint() { }

        public TestPoint(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public float X { get; private set; }
        public float Y { get; private set; }

        public void Deserialize(IXunitSerializationInfo info)
        {
            this.X = (float)info.GetValue<float>("X");
            this.Y = (float)info.GetValue<float>("Y");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("X", this.X);
            info.AddValue("Y", this.Y);
        }

        public override string ToString()
        {
            return $"({this.X}, {this.Y})";
        }

        public static implicit operator PointF(TestPoint p)
        {
            return new Vector2(p.X, p.Y);
        }
        public static implicit operator TestPoint(PointF p)
        {
            return new TestPoint(p.X, p.Y);
        }
    }
}
