﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphical.Geometry;
using Graphical.DataStructures;
using Graphical.Extensions;


namespace Graphical.Core
{
    public enum SweepLineType
    {
        Intersects,
        Boolean
    }

    public enum BooleanType
    {
        Intersection,
        Union,
        Differenece
    }

    public enum PolygonType
    {
        None = 0,
        Subject,
        Clip
    }

    public enum SweepEventLabel
    {
        Normal,
        NoContributing,
        SameTransition,
        DifferentTransition
    }

    /// <summary>
    /// Helper class to implement Bentley-Ottmann Algorithm for
    /// polygon self-intersections and boolean operations.
    /// </summary>
    public class SweepLine
    {
        #region Internal Properties
        internal SweepLineType sweepLineType;
        internal MinPriorityQ<SweepEvent> eventsQ;
        internal List<SweepEvent> eventsList;
        internal List<SweepEvent> activeEvents;
        internal IComparer<SweepEvent> verticalAscEventsComparer = new SortEventsVerticalAscendingComparer();
        internal Polygon subject;
        internal Polygon clip;
        #endregion

        #region Public Properties
        /// <summary>
        /// Returns the event below SweepEvent on index, null if none
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private SweepEvent BelowEvent(int index) { return (activeEvents.Any() && index > 0) ? activeEvents[index - 1] : null; }

        /// <summary>
        /// Returns the event above SweepEvent on index, null if none
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private SweepEvent AboveEvent(int index) { return (activeEvents.Any() && index + 1 < activeEvents.Count) ? activeEvents[index + 1] : null; }
        #endregion

        #region Internal Constructors
        internal SweepLine(List<Edge> edges, SweepLineType type)
        {
            this.sweepLineType = type;
            this.eventsList = new List<SweepEvent>(edges.Count * 2);
            this.activeEvents = new List<SweepEvent>(edges.Count);

            edges.ForEach(e => this.AddNewEvent(e));
        }
        // TODO: Seems that Q gets corrupted somehow. Check with Tests
        internal SweepLine(Polygon subject, Polygon clip, SweepLineType type)
        {
            this.sweepLineType = type;
            this.subject = subject;
            this.clip = clip;
            var totalEdges = subject.Edges.Count + clip.Edges.Count;
            this.eventsList = new List<SweepEvent>(totalEdges * 2);
            this.activeEvents = new List<SweepEvent>(totalEdges);

            subject.Edges.ForEach(e => this.AddNewEvent(e, PolygonType.Subject));
            clip.Edges.ForEach(e => this.AddNewEvent(e, PolygonType.Clip));
        }
        #endregion

        #region Public Constructors
        /// <summary>
        /// SweepLine constructor by a list of Edges
        /// </summary>
        /// <param name="edges"></param>
        /// <returns>SweepLine</returns>
        public static SweepLine ByEdges(List<Edge> edges)
        {
            return new SweepLine(edges, SweepLineType.Intersects);
        }

        /// <summary>
        /// SweepLine constructor by a list of gPolygons
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns>SweepLine</returns>
        public static SweepLine ByPolygons(List<Polygon> polygons)
        {
            return new SweepLine(
                polygons.SelectMany(p => p.Edges).ToList(),
                SweepLineType.Intersects);
        }

        public static SweepLine BySubjectClipPolygons(Polygon subject, Polygon clip)
        {
            return new SweepLine(subject, clip, SweepLineType.Boolean);
        }

        #endregion

        #region Internal Methods

        private void AddNewEvent(Edge edge, PolygonType polType = PolygonType.None)
        {
            SweepEvent swStart = new SweepEvent(edge.StartVertex, edge)
            {
                Label = SweepEventLabel.Normal
            };
            SweepEvent swEnd = new SweepEvent(edge.EndVertex, edge)
            {
                Label = SweepEventLabel.Normal
            };
            swStart.Pair = swEnd;
            swEnd.Pair = swStart;
            swStart.IsLeft = swStart < swEnd;
            swEnd.IsLeft = !swStart.IsLeft;

            if (polType != PolygonType.None)
            {
                swStart.polygonType = polType;
                swEnd.polygonType = polType;
            }

            eventsList.AddItemSorted(swStart);
            eventsList.AddItemSorted(swEnd);
        }

        public bool HasIntersection()
        {
            activeEvents = new List<SweepEvent>();
            foreach (SweepEvent sw in eventsList)
            {
                if (!activeEvents.Any())
                {
                    activeEvents.Add(sw);
                }
                else if (sw.IsLeft)
                {
                    int index = activeEvents.BisectIndex(sw, verticalAscEventsComparer);
                    activeEvents.Insert(index, sw);
                    SweepEvent belowEvent = BelowEvent(index);
                    SweepEvent aboveEvent = AboveEvent(index);

                    if (belowEvent != null && sw.Edge.Intersects(belowEvent.Edge))
                    {
                        if (!belowEvent.Edge.Contains(sw.Vertex) && !belowEvent.Edge.Contains(sw.Pair.Vertex))
                        {
                            return true;
                        }
                    }
                    if (aboveEvent != null && sw.Edge.Intersects(aboveEvent.Edge))
                    {
                        if (!aboveEvent.Edge.Contains(sw.Vertex) && !aboveEvent.Edge.Contains(sw.Pair.Vertex))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    int pairIndex = activeEvents.BisectIndex(sw.Pair, verticalAscEventsComparer) - 1;
                    SweepEvent belowEvent = BelowEvent(pairIndex);
                    SweepEvent aboveEvent = AboveEvent(pairIndex);

                    activeEvents.RemoveAt(pairIndex);
                    if (belowEvent != null && aboveEvent != null && belowEvent.Edge.Intersects(aboveEvent.Edge))
                    {
                        if (!belowEvent.Edge.Contains(aboveEvent.Vertex) && !belowEvent.Edge.Contains(aboveEvent.Pair.Vertex))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // TODO: Check no coplanar Edges.
        public List<Geometry.Geometry> GetIntersections()
        {
            activeEvents = new List<SweepEvent>();
            List<Geometry.Geometry> tempIntersections = new List<Geometry.Geometry>();
            this.eventsQ = new MinPriorityQ<SweepEvent>(this.eventsList.Capacity);
            this.eventsQ.AddRange(this.eventsList);

            while (eventsQ.Any())
            {
                SweepEvent nextEvent = eventsQ.Take();
                if (!activeEvents.Any())
                {
                    activeEvents.Add(nextEvent);
                }
                else if (nextEvent.IsLeft)
                {
                    int index = activeEvents.BisectIndex(nextEvent, verticalAscEventsComparer);
                    activeEvents.Insert(index, nextEvent);
                    SweepEvent belowEvent = BelowEvent(index);
                    SweepEvent aboveEvent = AboveEvent(index);

                    if (belowEvent != null) { ProcessIntersection(nextEvent, belowEvent, tempIntersections); }
                    if (aboveEvent != null) { ProcessIntersection(nextEvent, aboveEvent, tempIntersections); }
                }
                else
                {
                    int pairIndex = activeEvents.BisectIndex(nextEvent.Pair, verticalAscEventsComparer) - 1;
                    SweepEvent belowEvent = BelowEvent(pairIndex);
                    SweepEvent aboveEvent = AboveEvent(pairIndex);

                    activeEvents.RemoveAt(pairIndex);
                    if (belowEvent != null && aboveEvent != null) { ProcessIntersection(belowEvent, aboveEvent, tempIntersections); }
                }
            }

            return tempIntersections;
        }

        internal List<Polygon> ComputeBooleanOperation(BooleanType boolType)
        {
            EventChainer chain = new EventChainer(boolType);
            List<Polygon> computedPolygons = new List<Polygon>();

            this.eventsQ = new MinPriorityQ<SweepEvent>(this.eventsList.Capacity);
            this.eventsQ.AddRange(this.eventsList);

            // If one of the _polygonsDict is empty
            if (subject.Edges.Count * clip.Edges.Count == 0)
            {
                if (boolType == BooleanType.Differenece)
                {
                    computedPolygons.Add(subject);
                }
                else if (boolType == BooleanType.Intersection)
                {
                    computedPolygons.Add((subject.Edges.Count == 0) ? clip : subject);
                }
            }
            // If they don't intersect
            else if (!subject.Intersects(clip))
            {
                computedPolygons.Add(subject);
                if (boolType == BooleanType.Union)
                {
                    computedPolygons.Add(clip);
                }
            }
            else
            {
                while (eventsQ.Any())
                {
                    SweepEvent nextEvent = eventsQ.Take();
                    if (!activeEvents.Any())
                    {
                        activeEvents.Add(nextEvent);
                        ProcessInsideFlags(nextEvent, null);
                    }
                    else if (nextEvent.IsLeft)
                    {
                        int index = activeEvents.BisectIndex(nextEvent, verticalAscEventsComparer);
                        activeEvents.Insert(index, nextEvent);
                        SweepEvent belowEvent = BelowEvent(index);
                        SweepEvent aboveEvent = AboveEvent(index);
                        ProcessInsideFlags(nextEvent, belowEvent);

                        if (belowEvent != null) { ProcessIntersection(nextEvent, belowEvent); }
                        if (aboveEvent != null) { ProcessIntersection(nextEvent, aboveEvent); }
                    }
                    else
                    {
                        int pairIndex = activeEvents.BisectIndex(nextEvent.Pair, verticalAscEventsComparer) - 1;
                        SweepEvent belowEvent = BelowEvent(pairIndex);
                        SweepEvent aboveEvent = AboveEvent(pairIndex);

                        chain.Add(activeEvents[pairIndex]);
                        activeEvents.RemoveAt(pairIndex);
                        if (belowEvent != null && aboveEvent != null) { ProcessIntersection(belowEvent, aboveEvent); }
                    }

                    System.Diagnostics.Debug.WriteLine(String.Format("- {0} : \n\tIsInside: {1} \n\tInOut: {2}", nextEvent.ToString(), nextEvent.IsInside, nextEvent.InOut));
                }

                computedPolygons = chain.GetPolygons();
            }


            return computedPolygons;
        }

        internal void UpdateEventPair(SweepEvent swEvent, Vertex newVertexPair)
        {
            var pairEvent = swEvent.Pair;
            int index = eventsQ.IndexOf(pairEvent);

            // Update event and its pair with the new pair vertex 
            swEvent.UpdatePairVertex(newVertexPair);
            pairEvent.UpdatePairVertex(newVertexPair);

            // Update position of pairEvent in PriorityQ according to its new pair.
            eventsQ.UpdateAtIndex(index);

            // Add both new pairs to the PriorityQ
            eventsQ.Add(swEvent.Pair);
            eventsQ.Add(pairEvent.Pair);
        }

        internal void ProcessIntersection(SweepEvent next, SweepEvent prev, List<Geometry.Geometry> intersections = null)
        {
            Geometry.Geometry intersection = next.Edge.Intersection(prev.Edge);
            bool inserted = false;
            #region Is gVertex
            if (intersection is Vertex)
            {
                Vertex v = intersection as Vertex;
                // Intersection is between extremes Vertices
                foreach (SweepEvent sw in new List<SweepEvent>() { next, prev })
                {
                    if (!sw.Edge.Contains(v))
                    {
                        if (intersections != null && !inserted)
                        {
                            intersections.Add(v);
                            inserted = true;
                        }
                        UpdateEventPair(sw, v);
                    }
                }
            }
            #endregion
            #region Is Edge
            else if (intersection is Edge)
            {
                Edge e = intersection as Edge;

                // On Case 3 below, last half of prev event is added as intersection,
                // and on next loop it will be case 1 with the same edge, so this avoids duplicates
                if (intersections != null && (!intersections.Any() || !intersections.Last().Equals(e)))
                {
                    intersections.Add(e);
                    inserted = true;
                }

                // Case 1: events are coincident (same edge)
                // (prev)--------------------(prevPair)
                // (next)--------------------(nextPair)
                if (next.Equals(prev))
                {
                    // Setting nextEvent as not contributing instead of deleting it
                    // as doing so will make it's pair a lonely poor thing.
                    next.Label = SweepEventLabel.NoContributing;
                    prev.Label = next.InOut == prev.InOut ? SweepEventLabel.SameTransition : SweepEventLabel.DifferentTransition;
                }
                // Case 2: same start point, prev will be always shorter
                // as on PriorityQ it must have been sorted before next
                // (prev)----------(prevPair)
                // (next)--------------------(nextPair)
                else if (prev.Vertex.Equals(next.Vertex))
                {
                    // TODO: check this is true in all cases
                    Vertex dividingVtx = prev.Pair.Vertex;
                    UpdateEventPair(next, dividingVtx);
                }
                // Case 3: same end point, next will be always shorter
                // as on PriorityQ it must have been sorted after next
                // (prev)--------------------(prevPair)
                //        (next)-------------(nextPair)
                else if (prev.Pair.Vertex.Equals(next.Pair.Vertex))
                {
                    // TODO: check this is true in all cases
                    Vertex dividingVtx = next.Vertex;
                    UpdateEventPair(prev, dividingVtx);
                }
                // Case 4: events overlap
                // (prev)--------------------(prevPair)
                //        (next)--------------------(nextPair)
                else if (prev < next && prev.Pair < next.Pair)
                {
                    // TODO: check this is true in all cases
                    Vertex prevDividingVtx = next.Vertex;
                    Vertex nextDividingVtx = prev.Pair.Vertex;

                    UpdateEventPair(prev, prevDividingVtx);
                    UpdateEventPair(next, nextDividingVtx);
                }
                // Case 5: prev fully contains next
                // (prev)--------------------(prevPair)
                //        (next)---(nextPair)
                else if (prev < next && prev.Pair > next.Pair)
                {
                    next.Label = SweepEventLabel.NoContributing;
                    Vertex dividingVtx = next.Vertex;
                    Vertex pairDividingVtx = next.Pair.Vertex;

                    // Storing reference to prevPair before updating it
                    var prevPair = prev.Pair;

                    UpdateEventPair(prev, dividingVtx);
                    UpdateEventPair(prevPair, pairDividingVtx);
                }
                else
                {
                    throw new Exception("Case not contemplated? Damm!");
                }
            }
            #endregion 
            #endregion

        }

        /// <summary>
        /// This methods is call after intersection is calculated
        /// </summary>
        /// <param name="next"></param>
        /// <param name="prev"></param>
        internal void ProcessInsideFlags(SweepEvent next, SweepEvent prev)
        {
            // If prev is null, 
            if (prev == null)
            {
                next.IsInside = false;
                next.InOut = false;
            }
            // They intersect on the event's Vertices
            else if (next.polygonType == prev.polygonType)
            {
                next.IsInside = prev.IsInside;
                next.InOut = !prev.InOut;
            }
            // To Check
            else
            {
                next.IsInside = !prev.InOut;
                next.InOut = prev.IsInside;
            }

        }
    }

}
