using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Habrador_Computational_Geometry
{
    //From the report "An algorithm for generating constrained delaunay triangulations" by Sloan
    public static class ConstrainedDelaunaySloan
    {
        public static HalfEdgeData2 GenerateTriangulation(HashSet<MyVector2> points, List<MyVector2> constraints, bool shouldRemoveTriangles, HalfEdgeData2 triangleData)
        {
            //Start by generating a delaunay triangulation with all points, including the constraints
            if (constraints != null)
            {
                points.UnionWith(constraints);
            }

            //Generate the Delaunay triangulation with some algorithm
            //triangleData = _Delaunay.FlippingEdges(points);
            triangleData = _Delaunay.PointByPoint(points, triangleData);
            

            //Modify the triangulation by adding the constraints to the delaunay triangulation
            if (constraints != null)
            {
                triangleData = AddConstraints(triangleData, constraints, shouldRemoveTriangles);
            }

            //Debug.Log(triangleData.faces.Count);

            return triangleData;
        }



        //
        // Add the constraints to the delaunay triangulation
        //

        private static HalfEdgeData2 AddConstraints(HalfEdgeData2 triangleData, List<MyVector2> constraints, bool shouldRemoveTriangles)
        {
            //First create a list with all unique edges
            //In the half-edge data structure, we have for each edge an half edge going in each direction,
            //making it unneccessary to loop through all edges for intersection tests
            //The report suggest we should do a triangle walk, but it will not work if the mesh has holes
            List<HalfEdge2> uniqueEdges = triangleData.GetUniqueEdges();


            //The steps numbering is from the report
            //Step 1. Loop over each constrained edge. For each of these edges, do steps 2-4 
            for (int i = 0; i < constraints.Count; i++)
            {
                //Let each constrained edge be defined by the vertices:
                MyVector2 c_p1 = constraints[i];
                MyVector2 c_p2 = constraints[MathUtility.ClampListIndex(i + 1, constraints.Count)];

                //Check if this constraint already exists in the triangulation, 
                //if so we are happy and dont need to worry about this edge
                if (IsEdgeInListOfEdges(uniqueEdges, c_p1, c_p2))
                {
                    continue;
                }

                //Step 2. Find all edges in the current triangulation that intersects with this constraint
                Queue<HalfEdge2> intersectingEdges = FindIntersectingEdges_BruteForce(uniqueEdges, c_p1, c_p2);

                //Step 3. Remove intersecting edges by flipping triangles
                List<HalfEdge2> newEdges = RemoveIntersectingEdges(c_p1, c_p2, intersectingEdges);

                //Step 4. Try to restore delaunay triangulation 
                //Because we have constraints we will never get a delaunay triangulation
                RestoreDelaunayTriangulation(c_p1, c_p2, newEdges);
            }

            //Step 5. Remove superfluous triangles, such as the triangles "inside" the constraints  
            if (shouldRemoveTriangles)
            {
                RemoveSuperfluousTriangles(triangleData, constraints);
            }

            return triangleData;
        }



        //
        // Remove the edges that intersects with a constraint by flipping triangles
        //

        //The idea here is that all possible triangulations for a set of points can be found 
        //by systematically swapping the diagonal in each convex quadrilateral formed by a pair of triangles
        //So we will test all possible arrangements and will always find a triangulation which includes the constrained edge
        private static List<HalfEdge2> RemoveIntersectingEdges(MyVector2 v_i, MyVector2 v_j, Queue<HalfEdge2> intersectingEdges)
        {
            List<HalfEdge2> newEdges = new List<HalfEdge2>();

            int safety = 0;

            //While some edges still cross the constrained edge, do steps 3.1 and 3.2
            while (intersectingEdges.Count > 0)
            {
                safety += 1;

                if (safety > 100000)
                {
                    Debug.Log("Stuck in infinite loop when fixing constrained edges");

                    break;
                }

                //Step 3.1. Remove an edge from the list of edges that intersects the constrained edge
                HalfEdge2 e = intersectingEdges.Dequeue();

                //The vertices belonging to the two triangles
                MyVector2 v_k = e.v.position;
                MyVector2 v_l = e.prevEdge.v.position;
                MyVector2 v_3rd = e.nextEdge.v.position;
                //The vertex belonging to the opposite triangle and isn't shared by the current edge
                MyVector2 v_opposite_pos = e.oppositeEdge.nextEdge.v.position;

                //Step 3.2. If the two triangles don't form a convex quadtrilateral
                //place the edge back on the list of intersecting edges (because this edge cant be flipped) 
                //and go to step 3.1
                if (!_Geometry.IsQuadrilateralConvex(v_k, v_l, v_3rd, v_opposite_pos))
                {
                    intersectingEdges.Enqueue(e);

                    continue;
                }
                else
                {
                    //Flip the edge like we did when we created the delaunay triangulation
                    HalfEdgeHelpMethods.FlipTriangleEdge(e);

                    //The new diagonal is defined by the vertices
                    MyVector2 v_m = e.v.position;
                    MyVector2 v_n = e.prevEdge.v.position;

                    //If this new diagonal intersects with the constrained edge, add it to the list of intersecting edges
                    if (IsEdgeCrossingEdge(v_i, v_j, v_m, v_n))
                    {
                        intersectingEdges.Enqueue(e);
                    }
                    //Place it in the list of newly created edges
                    else
                    {
                        newEdges.Add(e);
                    }
                }
            }

            return newEdges;
        }



        //
        // Try to restore the delaunay triangulation by flipping newly created edges
        //

        //This process is similar to when we created the original delaunay triangulation
        //This step can maybe be skipped if you just want a triangulation and Ive noticed its often not flipping any triangles
        private static void RestoreDelaunayTriangulation(MyVector2 c_p1, MyVector2 c_p2, List<HalfEdge2> newEdges)
        {
            int safety = 0;

            int flippedEdges = 0;

            //Repeat 4.1 - 4.3 until no further swaps take place
            while (true)
            {
                safety += 1;

                if (safety > 100000)
                {
                    Debug.Log("Stuck in endless loop when delaunay after fixing constrained edges");

                    break;
                }

                bool hasFlippedEdge = false;

                //Step 4.1. Loop over each edge in the list of newly created edges
                for (int j = 0; j < newEdges.Count; j++)
                {
                    HalfEdge2 e = newEdges[j];

                    //Step 4.2. Let the newly created edge be defined by the vertices
                    MyVector2 v_k = e.v.position;
                    MyVector2 v_l = e.prevEdge.v.position;

                    //If this edge is equal to the constrained edge, then skip to step 4.1
                    //because we are not allowed to flip a constrained edge
                    if ((v_k.Equals(c_p1) && v_l.Equals(c_p2)) || (v_l.Equals(c_p1) && v_k.Equals(c_p2)))
                    {
                        continue;
                    }

                    //Step 4.3. If the two triangles that share edge v_k and v_l don't satisfy the delaunay criterion,
                    //so that a vertex of one of the triangles is inside the circumcircle of the other triangle, flip the edge
                    //The third vertex of the triangle belonging to this edge
                    MyVector2 v_third_pos = e.nextEdge.v.position;
                    //The vertice belonging to the triangle on the opposite side of the edge and this vertex is not a part of the edge
                    MyVector2 v_opposite_pos = e.oppositeEdge.nextEdge.v.position;

                    //Test if we should flip this edge
                    if (DelaunayMethods.ShouldFlipEdge(v_l, v_k, v_third_pos, v_opposite_pos))
                    {
                        //Flip the edge
                        hasFlippedEdge = true;

                        HalfEdgeHelpMethods.FlipTriangleEdge(e);

                        flippedEdges += 1;
                    }
                }

                //We have searched through all edges and havent found an edge to flip, so we cant improve anymore
                if (!hasFlippedEdge)
                {
                    Debug.Log("Found a constrained delaunay triangulation in " + flippedEdges + " flips");

                    break;
                }
            }
        }



        //
        // Remove all triangles that are inside the constraint
        //

        //This assumes the vertices in the constraint are ordered clockwise
        private static void RemoveSuperfluousTriangles(HalfEdgeData2 triangleData, List<MyVector2> constraints)
        {
            //This assumes we have at least 3 vertices in the constraint because we cant delete triangles inside a line
            if (constraints.Count < 3)
            {
                return;
            }

            HashSet<HalfEdgeFace2> trianglesToBeDeleted = FindTrianglesWithinConstraint(triangleData, constraints);

            //Delete the triangles
            foreach (HalfEdgeFace2 t in trianglesToBeDeleted)
            {
                HalfEdgeHelpMethods.DeleteTriangleFace(t, triangleData, true);
            }
        }



        //
        // Find which triangles are within a constraint
        //
       
        public static HashSet<HalfEdgeFace2> FindTrianglesWithinConstraint(HalfEdgeData2 triangleData, List<MyVector2> constraints)
        {
            HashSet<HalfEdgeFace2> trianglesToDelete = new HashSet<HalfEdgeFace2>();


            //Step 1. Find a triangle with an edge that shares an edge with the first constraint edge in the list 
            //Since both are clockwise we know we are "inside" of the constraint, so this is a triangle we should delete
            HalfEdgeFace2 borderTriangle = null;

            MyVector2 c_p1 = constraints[0];
            MyVector2 c_p2 = constraints[1];

            //Search through all triangles
            foreach (HalfEdgeFace2 t in triangleData.faces)
            {
                //The edges in this triangle
                HalfEdge2 e1 = t.edge;
                HalfEdge2 e2 = e1.nextEdge;
                HalfEdge2 e3 = e2.nextEdge;

                //Is any of these edges a constraint? If so we have find the first triangle
                if (e1.v.position.Equals(c_p2) && e1.prevEdge.v.position.Equals(c_p1))
                {
                    borderTriangle = t;

                    break;
                }
                if (e2.v.position.Equals(c_p2) && e2.prevEdge.v.position.Equals(c_p1))
                {
                    borderTriangle = t;

                    break;
                }
                if (e3.v.position.Equals(c_p2) && e3.prevEdge.v.position.Equals(c_p1))
                {
                    borderTriangle = t;

                    break;
                }
            }

            if (borderTriangle == null)
            {
                return null;
            }



            //Step 2. Find the rest of the triangles within the constraint by using a flood fill algorithm
            
            //Maybe better to first find all the other border triangles?

            //We know this triangle should be deleted
            trianglesToDelete.Add(borderTriangle);

            //Store the triangles we flood filling in this queue
            Queue<HalfEdgeFace2> trianglesToCheck = new Queue<HalfEdgeFace2>();

            //Start at the triangle we know is within the constraints
            trianglesToCheck.Enqueue(borderTriangle);

            int safety = 0;

            while (true)
            {
                safety += 1;

                if (safety > 100000)
                {
                    Debug.Log("Stuck in infinite loop when looking for triangles within constraint");

                    break;
                }

                //Stop if we are out of neighbors
                if (trianglesToCheck.Count == 0)
                {
                    break;
                }

                //Pick the first triangle in the list and investigate its neighbors
                HalfEdgeFace2 t = trianglesToCheck.Dequeue();

                //Investigate the triangles on the opposite sides of these edges
                HalfEdge2 e1 = t.edge;
                HalfEdge2 e2 = e1.nextEdge;
                HalfEdge2 e3 = e2.nextEdge;

                //A triangle is a neighbor within the constraint if:
                //- The neighbor is not an outer border meaning no neighbor exists
                //- If we have not already visited the neighbor
                //- If the edge between the neighbor and this triangle is not a constraint
                if (e1.oppositeEdge != null &&
                    !trianglesToDelete.Contains(e1.oppositeEdge.face) &&
                    !trianglesToCheck.Contains(e1.oppositeEdge.face) &&
                    !IsEdgeAConstraint(e1.v.position, e1.prevEdge.v.position, constraints))
                {
                    trianglesToCheck.Enqueue(e1.oppositeEdge.face);

                    trianglesToDelete.Add(e1.oppositeEdge.face);
                }
                if (e2.oppositeEdge != null &&
                    !trianglesToDelete.Contains(e2.oppositeEdge.face) &&
                    !trianglesToCheck.Contains(e2.oppositeEdge.face) &&
                    !IsEdgeAConstraint(e2.v.position, e2.prevEdge.v.position, constraints))
                {
                    trianglesToCheck.Enqueue(e2.oppositeEdge.face);

                    trianglesToDelete.Add(e2.oppositeEdge.face);
                }
                if (e3.oppositeEdge != null &&
                    !trianglesToDelete.Contains(e3.oppositeEdge.face) &&
                    !trianglesToCheck.Contains(e3.oppositeEdge.face) &&
                    !IsEdgeAConstraint(e3.v.position, e3.prevEdge.v.position, constraints))
                {
                    trianglesToCheck.Enqueue(e3.oppositeEdge.face);

                    trianglesToDelete.Add(e3.oppositeEdge.face);
                }
            }

            return trianglesToDelete;
        }


        
        //
        // Find edges that intersect with a constraint
        //

        //Method 1. Brute force by testing all unique edges
        //Find all edges of the current triangulation that intersects with the constraint edge between p1 and p2
        private static Queue<HalfEdge2> FindIntersectingEdges_BruteForce(List<HalfEdge2> uniqueEdges, MyVector2 c_p1, MyVector2 c_p2)
        {
            //Should be in a queue because we will later plop the first in the queue and add edges in the back of the queue 
            Queue<HalfEdge2> intersectingEdges = new Queue<HalfEdge2>();

            //Loop through all edges and see if they are intersecting with the constrained edge
            for (int i = 0; i < uniqueEdges.Count; i++)
            {
                //The edges the triangle consists of
                HalfEdge2 e = uniqueEdges[i];

                //The position the edge is going to
                MyVector2 e_p1 = e.v.position;
                //The position the edge is coming from
                MyVector2 e_p2 = e.prevEdge.v.position;

                //Is this edge intersecting with the constraint?
                if (IsEdgeCrossingEdge(e_p1, e_p2, c_p1, c_p2))
                {
                    //If so add it to the queue of edges
                    intersectingEdges.Enqueue(e);
                }
            }

            return intersectingEdges;
        }



        //Method 2. Triangulation walk
        //This assumes there are no holes in the mesh
        //And that we have a super-triangle around the triangulation
        private static void FindIntersectingEdges_TriangleWalk(HalfEdgeData2 triangleData, MyVector2 c_p1, MyVector2 c_p2, List<HalfEdge2> intersectingEdges)
        {
            //Step 1. Begin at a triangle connected to the constraint edges's vertex c_p1
            HalfEdgeFace2 f = null;

            foreach (HalfEdgeFace2 testFace in triangleData.faces)
            {
                //The edges the triangle consists of
                HalfEdge2 e1 = testFace.edge;
                HalfEdge2 e2 = e1.nextEdge;
                HalfEdge2 e3 = e2.nextEdge;

                //Does one of these edges include the first vertex in the constraint edge
                if (e1.v.position.Equals(c_p1) || e2.v.position.Equals(c_p1) || e3.v.position.Equals(c_p1))
                {
                    f = testFace;

                    break;
                }
            }


            
            //Step2. Walk around p1 until we find a triangle with an edge that intersects with the edge p1-p2
           

            //Step3. March from one triangle to the next in the general direction of p2
           
        }



        //
        // Edge stuff
        //

        //Are two edges the same edge?
        private static bool AreTwoEdgesTheSame(MyVector2 e1_p1, MyVector2 e1_p2, MyVector2 e2_p1, MyVector2 e2_p2)
        {
            //Is e1_p1 part of this constraint?
            if ((e1_p1.Equals(e2_p1) || e1_p1.Equals(e2_p2)))
            {
                //Is e1_p2 part of this constraint?
                if ((e1_p2.Equals(e2_p1) || e1_p2.Equals(e2_p2)))
                {
                    return true;
                }
            }

            return false;
        }



        //Is an edge (between p1 and p2) in a list with edges
        private static bool IsEdgeInListOfEdges(List<HalfEdge2> edges, MyVector2 p1, MyVector2 p2)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                //The vertices positions of the current triangle
                MyVector2 e_p1 = edges[i].v.position;
                MyVector2 e_p2 = edges[i].prevEdge.v.position;

                //Check if edge has the same coordinates as the constrained edge
                //We have no idea about direction so we have to check both directions
                if (AreTwoEdgesTheSame(p1, p2, e_p1, e_p2))
                {
                    return true;
                }
            }

            return false;
        }



        //Is an edge between p1 and p2 a constraint?
        private static bool IsEdgeAConstraint(MyVector2 p1, MyVector2 p2, List<MyVector2> constraints)
        {
            for (int i = 0; i < constraints.Count; i++)
            {
                MyVector2 c_p1 = constraints[i];
                MyVector2 c_p2 = constraints[MathUtility.ClampListIndex(i + 1, constraints.Count)];

                if (AreTwoEdgesTheSame(p1, p2, c_p1, c_p2))
                {
                    return true;
                }
            }

            return false;
        }



        //Is an edge crossing another edge? 
        private static bool IsEdgeCrossingEdge(MyVector2 e1_p1, MyVector2 e1_p2, MyVector2 e2_p1, MyVector2 e2_p2)
        {
            //We will here run into floating point precision issues so we have to be careful
            //To solve that you can first check the end points 
            //and modify the line-line intersection algorithm to include a small epsilon

            //First check if the edges are sharing a point, if so they are not crossing
            if (e1_p1.Equals(e2_p1) || e1_p1.Equals(e2_p2) || e1_p2.Equals(e2_p1) || e1_p2.Equals(e2_p2))
            {
                return false;
            }

            //Then check if the lines are intersecting
            if (!_Intersections.LineLine(e1_p1, e1_p2, e2_p1, e2_p2, shouldIncludeEndPoints: false))
            {
                return false;
            }

            return true;
        }
    }
}
