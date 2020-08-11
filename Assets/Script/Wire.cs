using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Habrador_Computational_Geometry;
using System.Linq;

public class Wire : MonoBehaviour
{
    private Collider2D _collider2D;

    [SerializeField]
    private MagneticField[] magneticFields;
    private Dictionary<MagneticField,float> areas = new Dictionary<MagneticField,float>();

    [SerializeField]
    private Vector3 speed;
    [SerializeField]
    private Vector3 vspeed;

    [SerializeField]
    private LineRenderer line;

    private float prevAreaSize = 0;

    private void Awake()
    {
        _collider2D = gameObject.GetComponent<Collider2D>();
    }

    int indexNum = 0;
    float currentTime = 0;
    float totalTime = 0;
    private void move()
    {
        var deltaTime = Time.deltaTime;
        gameObject.transform.position += speed*deltaTime;
        speed += vspeed*Time.deltaTime;

        float induced = 0;
        foreach(var magneticField in magneticFields)
        {
            induced += CalculateInducedPower(magneticField,deltaTime);
        }

        Debug.Log(induced);

        line.positionCount++;
        line.SetPosition(line.positionCount - 1, new Vector3(totalTime, induced/2, 0));

        totalTime += deltaTime*3;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        var vertex = GetVertexsFromParent(transform);
        DrawPolygon(vertex);
        Gizmos.color = Color.blue;
        var target = GetVertexsFromParent(magneticFields[0].transform);
        DrawPolygon(target);

        // foreach(var poly in GetGreinerHormann(vertex,target))
        // {
        //     Gizmos.color = Color.green;

        //     DrawPolygon(poly);
        // }
    }

    void Update()
    {
        move();
    }

    List<MyVector2> GetVertexsFromParent(Transform parent)
    {
        var vertex = new List<MyVector2>();

        for (int i = 0; i < parent.childCount; i++)
        {
            var pos = parent.GetChild(i).transform.position;
            vertex.Add(new MyVector2(pos.x, pos.y));
        }

        return vertex;
    }

    private List<List<MyVector2>> GetGreinerHormann(List<MyVector2> poly, List<MyVector2> clipPoly)
    {
        //Normalize to range 0-1
        //We have to use all data to normalize
        List<MyVector2> allPoints = new List<MyVector2>();
        allPoints.AddRange(poly);
        allPoints.AddRange(clipPoly);

        //In this case we can get back multiple parts of the polygon because one of the 
        //polygons doesnt have to be convex
        //If you pick boolean operation: intersection you should get the same result as with the Sutherland-Hodgman
        List<List<MyVector2>> finalPolygon = GreinerHormann.ClipPolygons(poly, clipPoly, BooleanOperation.Intersection);

//        Debug.Log("Total polygons: " + finalPolygon.Count);

        return finalPolygon;
    }

    void DrawPolygon(List<MyVector2> points)
    {
        MyVector2? prevPoint = null;
        foreach (var point in points)
        {
            if (prevPoint != null)
            {
                Gizmos.DrawLine(new Vector3(prevPoint.Value.x, prevPoint.Value.y, 0),
                    new Vector3(point.x, point.y, 0));
            }

            prevPoint = point;
        }

        Gizmos.DrawLine(new Vector3(points[points.Count - 1].x, points[points.Count - 1].y, 0),
            new Vector3(points[0].x, points[0].y, 0));
    }

    float CalculateInducedPower(MagneticField magneticField,float deltaTime)
    {
        var vertex = GetVertexsFromParent(transform);
        var target = GetVertexsFromParent(magneticField.transform);

        float areaSum = 0;
        foreach (var poly in GetGreinerHormann(vertex, target))
        {
            var points = new List<MyVector2>(poly);
            points.Add(points[0]);
            var area = Mathf.Abs(points.Take(points.Count - 1)
               .Select((p, i) => (points[i + 1].x - p.x) * (points[i + 1].y + p.y))
               .Sum() / 2);
            Gizmos.color = Color.green;
            //DrawPolygon(poly);
            areaSum+=area;
        }
        Debug.Log("크기"+areaSum);
        if(!areas.ContainsKey(magneticField))
        {
            areas[magneticField] = 0;
        }
        var deltaArea = areaSum - areas[magneticField];
Debug.Log("변한크기"+deltaArea);
        areas[magneticField] = areaSum;

        return deltaArea/deltaTime * magneticField.strength * (magneticField.isFront?1:-1);
    }
}

