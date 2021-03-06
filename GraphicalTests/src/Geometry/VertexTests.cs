﻿using NUnit.Framework;
using Graphical.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphical.Geometry.Tests
{
    [TestFixture]
    public class VertexTests
    {
        [Test]
        public void OnEdgeTest()
        {
            var a = Vertex.ByCoordinates(0, 0, 0);
            var a2 = Vertex.ByCoordinates(10, 0, 0);
            var b = Vertex.ByCoordinates(5, 5, 5);
            var c = Vertex.ByCoordinates(10, 10, 10);
            var d = Vertex.ByCoordinates(3, 4, 6);
            var e = Vertex.ByCoordinates(5, 0.00001, 0);
            var e2 = Vertex.ByCoordinates(5, 0.1, 0);

            Assert.IsTrue(b.OnEdge(a, b)); // Same end point
            Assert.IsTrue(b.OnEdge(a, c)); // On edge
            Assert.IsFalse(c.OnEdge(a, b)); // Colinear but not in between.
            Assert.IsFalse(d.OnEdge(a, c)); // No Colinear
            Assert.IsTrue(e.OnEdge(a, a2)); // Almost colinear, smaller than threshold
            Assert.IsFalse(e2.OnEdge(a, a2)); // Almost colinear, bigger than threshold
        }

        [Test]
        public void CoplanarTest()
        {
            var a = Vertex.ByCoordinates(0, 0, 0);
            var b = Vertex.ByCoordinates(0, 10, 0);
            var c = Vertex.ByCoordinates(10, 10, 0);
            var d = Vertex.ByCoordinates(15, 25, 0);
            var e = Vertex.ByCoordinates(0, 50, 0);
            var f = Vertex.ByCoordinates(15, 15, 0.5);
            
            Assert.IsTrue(Vertex.Coplanar(new List<Vertex>() { a, b}));
            Assert.IsTrue(Vertex.Coplanar(new List<Vertex>() { a, b, c }));
            Assert.IsFalse(Vertex.Coplanar(new List<Vertex>() { a, b, c, d, e, f}));
            Assert.IsFalse(Vertex.Coplanar(new List<Vertex>() { c, d, e, f}));
        }
        
        [Test]
        public void ConvexHullTest()
        {
            var a = Vertex.ByCoordinates(1, 1);
            var b = Vertex.ByCoordinates(1, 3);
            var c = Vertex.ByCoordinates(2, 2);
            var d = Vertex.ByCoordinates(2, 3);
            var e = Vertex.ByCoordinates(3, 2);
            var f = Vertex.ByCoordinates(3, 0);
            var g = Vertex.ByCoordinates(4, 1);
            var h = Vertex.ByCoordinates(4, 3);
            var i = Vertex.ByCoordinates(5, 1);
            var j = Vertex.ByCoordinates(5, 4);
            var k = Vertex.ByCoordinates(6, 2);

            var convexHull = Vertex.ConvexHull(new List<Vertex>() { a, b, c, d, e, f, g, h, i, j, k });

            Assert.AreEqual(6, convexHull.Count);
        }

    }
}