using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.DesignScript.Geometry;


namespace DynamoMeshBar
{
    public class DynamoMeshBar
    {
        public static Mesh CreateMeshBarFromPoints( Point startPoint, Point endPoint, double radius = 8, int edgeCount = 6)
        {
            Line distance = Line.ByStartPointEndPoint(startPoint, endPoint);
            Vector tangentVector = distance.Direction;
            Circle startCircle = Circle.ByCenterPointRadiusNormal(startPoint, radius, tangentVector);
            Circle endCircle = Circle.ByCenterPointRadiusNormal(endPoint, radius, tangentVector);

            List<Point> vertexList = new List<Point>();
            List<IndexGroup> indiceList = new List<IndexGroup>();

            for (int i = 0; i < edgeCount; i++)//start circle
            {
                vertexList.Add(startCircle.PointAtParameter(i / (double)edgeCount));//not casting to double will fail the division silently returning 0

            }
            for (int i = 0; i < edgeCount; i++)//end circle
            {
                vertexList.Add(endCircle.PointAtParameter(i / (double) edgeCount));
            }

            for (int i = 0; i < edgeCount; i++)//facets
            {
                if (i < edgeCount - 1)
                {
                    indiceList.Add(IndexGroup.ByIndices((uint)i, (uint)i + 1, (uint)edgeCount + (uint)i + 1));//clockwise orientation of vertices [startCircle[0], startCircle[1], endCircle[0],
                    indiceList.Add(IndexGroup.ByIndices((uint)edgeCount + (uint)i + 1, (uint)edgeCount + (uint)i, //clockwise orientation of vertices [endCircle[1], endCircle[0], startCircle[0],
                        (uint) i));
                }
                else if (i == edgeCount - 1) //stitch last strip to first
                {
                    indiceList.Add(IndexGroup.ByIndices((uint)i, (uint)0, (uint)edgeCount + (uint)0));
                    indiceList.Add(IndexGroup.ByIndices((uint)edgeCount + (uint)0, (uint)edgeCount + (uint)i,
                        (uint)i));
                }
            }
            

            Mesh extrudedBar = Mesh.ByPointsFaceIndices(vertexList,indiceList);


            //cleanup local vars
            distance.Dispose();
            tangentVector.Dispose();
            startCircle.Dispose();
            endCircle.Dispose();
            vertexList.Clear();
            indiceList.Clear();

            return extrudedBar;
        }

        public static Mesh CreateMeshBarFromLine(Line line, double radius = 8, int edgeCount = 6)
        {

            return CreateMeshBarFromPoints(line.StartPoint, line.EndPoint,radius,edgeCount);

        }

        public static List<Line> CreateLinesFromMesh(Mesh mesh, int limit)
        {
            List<Line> lines = new List<Line>();

            // use a using statement here to ensure copy of vertices goes out of scope, or just not have a local copy at all?
            Point[] vertices = mesh.VertexPositions;
            IndexGroup[] indices = mesh.FaceIndices;

            //need edge list here but it's not avail in DS mesh class exposure. edge list would be much better than traversing indices 

            foreach (IndexGroup index in indices)
            {
                if (lines.Count < limit)
                {


                    Line line1 = Line.ByStartPointEndPoint(vertices[index.A], vertices[index.B]);
                    Line line2 = Line.ByStartPointEndPoint(vertices[index.B], vertices[index.C]);
                    Line line3 = Line.ByStartPointEndPoint(vertices[index.C], vertices[index.A]);

                    //this is ineffiecent and will result in overlapping lines if using a straight list, need to use HashSet instead 
                    //Line line1 = Line.ByStartPointEndPoint(mesh.VertexPositions[index.A], mesh.VertexPositions[index.B]);
                    //Line line2 = Line.ByStartPointEndPoint(mesh.VertexPositions[index.B], mesh.VertexPositions[index.C]);
                    //Line line3 = Line.ByStartPointEndPoint(mesh.VertexPositions[index.C], mesh.VertexPositions[index.A]);
                    lines.Add(line1);
                    lines.Add(line2);
                    lines.Add(line3);

                    //cleanup local vars
                    //line1.Dispose();
                    //line2.Dispose();
                    //line3.Dispose();

                    if (index.Count == 4)
                    {
                        Line line4 = Line.ByStartPointEndPoint(vertices[index.C], vertices[index.D]);
                        //Line line4 = Line.ByStartPointEndPoint(mesh.VertexPositions[index.C],
                        //    mesh.VertexPositions[index.D]);
                        lines.Add(line4);
                        //line4.Dispose();
                    }
                }
            }

            return lines;//.ToList();
        }

        public static Mesh CreateMeshBarFromEdge(Edge edge, double radius = 8, int edgeCount = 6)
        {

            return CreateMeshBarFromPoints(edge.StartVertex.PointGeometry, edge.EndVertex.PointGeometry, radius, edgeCount);

        }
    }
}
