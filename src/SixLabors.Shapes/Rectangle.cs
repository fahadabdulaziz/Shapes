﻿// <copyright file="Rectangle.cs" company="Scott Williams">
// Copyright (c) Scott Williams and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace SixLabors.Shapes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;

    /// <summary>
    /// A way of optimizing drawing rectangles.
    /// </summary>
    /// <seealso cref="SixLabors.Shapes.IShape" />
    public class Rectangle : IShape
    {
        private readonly Vector2 topLeft;
        private readonly Vector2 bottomRight;
        private readonly ImmutableArray<Vector2> points;
        private readonly ImmutableArray<IPath> pathCollection;
        private readonly float halfLength;
        private readonly float length;

        private readonly RectanglePath path;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle" /> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Rectangle(float x, float y, float width, float height)
            : this(new Vector2(x, y), new Size(width, height))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle" /> class.
        /// </summary>
        /// <param name="topLeft">The top left.</param>
        /// <param name="bottomRight">The bottom right.</param>
        public Rectangle(Vector2 topLeft, Vector2 bottomRight)
        {
            this.Location = topLeft;
            this.topLeft = topLeft;
            this.bottomRight = bottomRight;
            this.Size = new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

            this.points = ImmutableArray.Create(new Vector2[4]
            {
                this.topLeft,
                new Vector2(this.bottomRight.X, this.topLeft.Y),
                this.bottomRight,
                new Vector2(this.topLeft.X, this.bottomRight.Y)
            });

            this.halfLength = this.Size.Width + this.Size.Height;
            this.length = this.halfLength * 2;
            this.path = new RectanglePath(this);
            this.pathCollection = ImmutableArray.Create<IPath>(this.path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="size">The size.</param>
        public Rectangle(Vector2 location, Size size)
            : this(location, location + size.ToVector2())
        {
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public Vector2 Location { get; }

        /// <summary>
        /// Gets the left.
        /// </summary>
        /// <value>
        /// The left.
        /// </value>
        public float Left => this.topLeft.X;

        /// <summary>
        /// Gets the X.
        /// </summary>
        /// <value>
        /// The X.
        /// </value>
        public float X => this.topLeft.X;

        /// <summary>
        /// Gets the right.
        /// </summary>
        /// <value>
        /// The right.
        /// </value>
        public float Right => this.bottomRight.X;

        /// <summary>
        /// Gets the top.
        /// </summary>
        /// <value>
        /// The top.
        /// </value>
        public float Top => this.topLeft.Y;

        /// <summary>
        /// Gets the Y.
        /// </summary>
        /// <value>
        /// The Y.
        /// </value>
        public float Y => this.topLeft.Y;

        /// <summary>
        /// Gets the bottom.
        /// </summary>
        /// <value>
        /// The bottom.
        /// </value>
        public float Bottom => this.bottomRight.Y;

        /// <summary>
        /// Gets the bounding box of this shape.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        Rectangle IShape.Bounds => this;

        /// <summary>
        /// Gets the paths that make up this shape
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        ImmutableArray<IPath> IShape.Paths => this.pathCollection;

        /// <summary>
        /// Gets the maximum number intersections that a shape can have when testing a line.
        /// </summary>
        /// <value>
        /// The maximum intersections.
        /// </value>
        int IShape.MaxIntersections => 4;

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public Size Size { get; private set; }

        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>
        /// The center.
        /// </value>
        public Vector2 Center => this.topLeft + (this.bottomRight / 2);

        /// <summary>
        /// Determines if the specified point is contained within the rectangular region defined by
        /// this <see cref="Rectangle" />.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// The <see cref="bool" />
        /// </returns>
        public bool Contains(Vector2 point)
        {
            return Vector2.Clamp(point, this.topLeft, this.bottomRight) == point;
        }

        /// <summary>
        /// the distance of the point from the outline of the shape, if the value is negative it is inside the polygon bounds
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// Returns the distance from the shape to the point
        /// </returns>
        public float Distance(Vector2 point)
        {
            bool insidePoly;
            PointInfo result = this.Distance(point, true, out insidePoly);

            // invert the distance from path when inside
            return insidePoly ? -result.DistanceFromPath : result.DistanceFromPath;
        }

        /// <summary>
        /// Based on a line described by <paramref name="start" /> and <paramref name="end" />
        /// populate a buffer for all points on the polygon that the line intersects.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>
        /// The locations along the line segment that intersect with the edges of the shape.
        /// </returns>
        public IEnumerable<Vector2> FindIntersections(Vector2 start, Vector2 end)
        {
            var buffer = new Vector2[2];
            var c = this.FindIntersections(start, end, buffer, 2, 0);
            switch (c)
            {
                case 2:
                    return buffer;
                case 1:
                    return new[] { buffer[0] };
                default:
                    return Enumerable.Empty<Vector2>();
            }
        }

        /// <summary>
        /// Based on a line described by <paramref name="start"/> and <paramref name="end"/>
        /// populate a buffer for all points on the edges of the <see cref="Rectangle"/>
        /// that the line intersects.
        /// </summary>
        /// <param name="start">The start point of the line.</param>
        /// <param name="end">The end point of the line.</param>
        /// <param name="buffer">The buffer that will be populated with intersections.</param>
        /// <param name="count">The count.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>
        /// The number of intersections populated into the buffer.
        /// </returns>
        public int FindIntersections(Vector2 start, Vector2 end, Vector2[] buffer, int count, int offset)
        {
            int discovered = 0;
            Vector2 startPoint = Vector2.Clamp(start, this.topLeft, this.bottomRight);
            Vector2 endPoint = Vector2.Clamp(end, this.topLeft, this.bottomRight);

            // start doesn't change when its inside the shape thus not crossing
            if (startPoint != start)
            {
                if (startPoint == Vector2.Clamp(startPoint, start, end))
                {
                    // if start closest is within line then its a valid point
                    discovered++;
                    buffer[offset++] = startPoint;
                }
            }

            // end didn't change it must not intercept with an edge
            if (endPoint != end)
            {
                if (endPoint == Vector2.Clamp(endPoint, start, end))
                {
                    // if start closest is within line then its a valid point
                    discovered++;
                    buffer[offset] = endPoint;
                }
            }

            return discovered;
        }

        /// <summary>
        /// Transforms the rectangle using specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        /// A new shape with the matrix applied to it.
        /// </returns>
        public IShape Transform(Matrix3x2 matrix)
        {
            if (matrix.IsIdentity)
            {
                return this;
            }

            // rectangles may be rotated and skewed which means they will then nedd representing by a polygon
            return new Polygon(new LinearLineSegment(this.points).Transform(matrix));
        }

        private PointInfo Distance(Vector2 point, bool getDistanceAwayOnly, out bool isInside)
        {
            // point in rectangle
            // if after its clamped by the extreams its still the same then it must be inside :)
            Vector2 clamped = Vector2.Clamp(point, this.topLeft, this.bottomRight);
            isInside = clamped == point;

            float distanceFromEdge = float.MaxValue;
            float distanceAlongEdge = 0f;

            if (isInside)
            {
                // get the absolute distances from the extreams
                Vector2 topLeftDist = Vector2.Abs(point - this.topLeft);
                Vector2 bottomRightDist = Vector2.Abs(point - this.bottomRight);

                // get the min components
                Vector2 minDists = Vector2.Min(topLeftDist, bottomRightDist);

                // and then the single smallest (dont have to worry about direction)
                distanceFromEdge = Math.Min(minDists.X, minDists.Y);

                if (!getDistanceAwayOnly)
                {
                    // we need to make clamped the closest point
                    if (this.topLeft.X + distanceFromEdge == point.X)
                    {
                        // closer to lhf
                        clamped.X = this.topLeft.X; // y is already the same

                        // distance along edge is length minus the amout down we are from the top of the rect
                        distanceAlongEdge = this.length - (clamped.Y - this.topLeft.Y);
                    }
                    else if (this.topLeft.Y + distanceFromEdge == point.Y)
                    {
                        // closer to top
                        clamped.Y = this.topLeft.Y; // x is already the same

                        distanceAlongEdge = clamped.X - this.topLeft.X;
                    }
                    else if (this.bottomRight.Y - distanceFromEdge == point.Y)
                    {
                        // closer to bottom
                        clamped.Y = this.bottomRight.Y; // x is already the same

                        distanceAlongEdge = (this.bottomRight.X - clamped.X) + this.halfLength;
                    }
                    else if (this.bottomRight.X - distanceFromEdge == point.X)
                    {
                        // closer to rhs
                        clamped.X = this.bottomRight.X; // x is already the same
                        distanceAlongEdge = (clamped.Y - this.topLeft.Y) + this.Size.Width;
                    }
                }
            }
            else
            {
                // clamped is the point on the path thats closest no matter what
                distanceFromEdge = (clamped - point).Length();

                if (!getDistanceAwayOnly)
                {
                    // we need to figure out whats the cloests edge now and thus what distance/poitn is closest
                    if (this.topLeft.X == clamped.X)
                    {
                        // distance along edge is length minus the amout down we are from the top of the rect
                        distanceAlongEdge = this.length - (clamped.Y - this.topLeft.Y);
                    }
                    else if (this.topLeft.Y == clamped.Y)
                    {
                        distanceAlongEdge = clamped.X - this.topLeft.X;
                    }
                    else if (this.bottomRight.Y == clamped.Y)
                    {
                        distanceAlongEdge = (this.bottomRight.X - clamped.X) + this.halfLength;
                    }
                    else if (this.bottomRight.X == clamped.X)
                    {
                        distanceAlongEdge = (clamped.Y - this.topLeft.Y) + this.Size.Width;
                    }
                }
            }

            if (distanceAlongEdge == this.length)
            {
                distanceAlongEdge = 0;
            }

            return new PointInfo
            {
                SearchPoint = point,
                DistanceFromPath = distanceFromEdge,
                ClosestPointOnPath = clamped,
                DistanceAlongPath = distanceAlongEdge
            };
        }

        private class RectanglePath : IWrapperPath
        {
            private readonly Rectangle rect;

            public RectanglePath(Rectangle rect)
            {
                this.rect = rect;
            }

            public Rectangle Bounds => this.rect;

            public float Length => this.rect.length;

            public PointInfo Distance(Vector2 point)
            {
                    bool inside; // dont care about inside/outside for paths just distance
                    return this.rect.Distance(point, false, out inside);
            }

            public ImmutableArray<Vector2> Flatten() => this.rect.points;

            public IPath Transform(Matrix3x2 matrix) => this.rect.Transform(matrix).Paths[0];

            public IShape AsShape() => this.rect;
        }
    }
}