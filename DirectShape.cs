using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.DesignScript.Geometry;
using DynamoServices;
using Autodesk.Revit.UI;
using RevitServices.Persistence;
using RevitServices.Transactions;


namespace DynamoMeshBar
{



    // DirectShape code from Jeremy Tammik's example https://github.com/jeremytammik/DirectObjLoader 

    public class DirectShapeFromDSMesh
    {
        static ElementId _categoryId = new ElementId(
  BuiltInCategory.OST_GenericModel);



        /// <summary>
        /// Create a new DirectShape element from given
        /// list of faces and return the number of faces
        /// processed.
        /// Return -1 if a face vertex index exceeds the
        /// total number of available vertices, 
        /// representing a fatal error.
        /// </summary>
        public static DirectShape NewDirectShape(
          //List<XYZ> vertices,
          Autodesk.DesignScript.Geometry.Mesh mesh,
          //FaceCollection faces,
          //UIDocument UIdoc,
          //ElementId graphicsStyleId,
          //string appGuid,
          string shapeName)
        {
            int nFaces = 0;
            int nFacesFailed = 0;

            Document doc = DocumentManager.Instance.CurrentDBDocument;
                
            string appGuid = doc.Application.ActiveAddInId.GetGUID().ToString();

            // Retrieve "<Sketch>" graphics style, 
            // if it exists.

            FilteredElementCollector collector
              = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle));

            GraphicsStyle style
              = collector.Cast<GraphicsStyle>()
                .FirstOrDefault<GraphicsStyle>(gs
                  => gs.Name.Equals("<Sketch>"));

            ElementId graphicsStyleId = null;

            if (style != null)
            {
                graphicsStyleId = style.Id;
            }


            TessellatedShapeBuilder builder
              = new TessellatedShapeBuilder();

            builder.LogString = shapeName;

            List<Autodesk.DesignScript.Geometry.Point> corners = new List<Autodesk.DesignScript.Geometry.Point>(4);
            List<XYZ> XYZcorners = new List<XYZ>(4);

            builder.OpenConnectedFaceSet(false);

           // foreach (Face f in faces)
            //{
            builder.LogInteger = nFaces;

            if (corners.Capacity < mesh.FaceIndices.Length)
            {
                corners = new List<Autodesk.DesignScript.Geometry.Point>(mesh.FaceIndices.Length);
                XYZcorners = new List<XYZ>(mesh.FaceIndices.Length);
            }

            corners.Clear();
            XYZcorners.Clear();

            foreach (IndexGroup i in mesh.FaceIndices)
            {
                //Debug.Assert(vertices.Count > i.vertex,
                //  "how can the face vertex index be larger "
                //  + "than the total number of vertices?");

                corners.Add(mesh.VertexPositions[i.A]);
                XYZcorners.Add(new XYZ(mesh.VertexPositions[(int) i.A].X, mesh.VertexPositions[(int) i.A].Y,
                    mesh.VertexPositions[(int) i.A].Z));
                corners.Add(mesh.VertexPositions[(int) i.B]);
                XYZcorners.Add(new XYZ(mesh.VertexPositions[(int) i.B].X, mesh.VertexPositions[(int) i.B].Y,
                    mesh.VertexPositions[(int) i.B].Z));
                corners.Add(mesh.VertexPositions[(int) i.C]);
                XYZcorners.Add(new XYZ(mesh.VertexPositions[(int) i.C].X, mesh.VertexPositions[(int) i.C].Y,
                    mesh.VertexPositions[(int) i.C].Z));

                if (i.Count > 3)
                {
                    corners.Add(mesh.VertexPositions[(int) i.D]);
                    XYZcorners.Add(new XYZ(mesh.VertexPositions[(int) i.D].X, mesh.VertexPositions[(int) i.D].Y,
                        mesh.VertexPositions[(int) i.D].Z));
                }

            }



            try
            {
                builder.AddFace(new TessellatedFace(XYZcorners,
                    ElementId.InvalidElementId));

                ++nFaces;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException ex)
            {
                // Remember something went wrong here.

                ++nFacesFailed;

                Debug.Print(
                    "Revit API argument exception {0}\r\n"
                    + "Failed to add face with {1} corners: {2}",
                    ex.Message, corners.Count,
                    string.Join(", ",
                        XYZcorners.Select<XYZ, string>(
                            p => Util.PointString(p))));
            }
            //}

            builder.CloseConnectedFaceSet();

            // Refer to StlImport sample for more clever 
            // handling of target and fallback and the 
            // possible combinations.

            TessellatedShapeBuilderResult r
              = builder.Build(
                TessellatedShapeBuilderTarget.Mesh,
                TessellatedShapeBuilderFallback.Salvage,
                graphicsStyleId);

            ConversionLog myLog = new ConversionLog("c:\\conversionLogFileName.txt");

            DataConversionMonitorScope myLoggingScope = new DataConversionMonitorScope(myLog);

            TessellatedShapeBuilderOutcome testOutcome  = r.Outcome;
            IList<GeometryObject> test = r.GetGeometricalObjects();


            try
            {
                TransactionManager.Instance.EnsureInTransaction(DocumentManager.Instance.CurrentDBDocument);

                DirectShape ds = DirectShape.CreateElement(
                    doc, _categoryId, appGuid, shapeName);

                ds.SetShape(r.GetGeometricalObjects());
                ds.Name = shapeName;

                Debug.Print(
                    "Shape '{0}': added {1} face{2}, {3} face{4} failed.",
                    shapeName, nFaces, Util.PluralSuffix(nFaces),
                    nFacesFailed, Util.PluralSuffix(nFacesFailed));

                TransactionManager.Instance.TransactionTaskDone();

                return ds;
            }
            catch(Exception ex)
            {
                Debug.Print(
                    "Problem with adding DirectShape: " + ex.Message.ToString());

                return null;
            }

        }
    }
}
