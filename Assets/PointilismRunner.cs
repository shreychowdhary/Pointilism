using System;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Camera))]

public class PointilismRunner : MonoBehaviour
{

    public ComputeShader shader;
    public Shader drawLineShader;

    public int skipWidth;

    public float lineLength;
    public float lineThickness;

    private struct Vertex
    {
        public Vector2 pos;
        public Vector4 color;
    }
    public Material drawLinesMaterial;

    private RenderTexture tempSource = null;
    // we need this intermediate render texture to access the data   
    private RenderTexture tempDestination = null;
    // we need this intermediate render texture for two reasons:
    // 1. destination of OnRenderImage might be null 
    // 2. we cannot set enableRandomWrite on destination

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer argsBuffer;
    private int handleSobel;


    void Start()
    {
        if (shader == null)
        {
            Debug.Log("Shader missing.");
            enabled = false;
            return;
        }

        handleSobel = shader.FindKernel("Sobel");

        if (handleSobel < 0)
        {
            Debug.Log("Initialization failed.");
            enabled = false;
            return;
        }
        drawLinesMaterial = new Material(drawLineShader);
        vertexBuffer = new ComputeBuffer(6, sizeof(float) * (2 + 4));
        argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[4] { 6, 960 * 540 / 100, 0, 0 });
    }

    void OnDestroy()
    {
        if (tempSource != null)
        {
            tempSource.Release();
            tempSource = null;
        }
        if (tempDestination != null)
        {
            tempDestination.Release();
            tempDestination = null;
        }
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
        }
        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shader == null || handleSobel < 0 || source == null)
        {
            Graphics.Blit(source, destination); // just copy
            return;
        }

        // do we need to create a new temporary source texture?
        if (tempSource == null || source.width != tempSource.width
           || source.height != tempSource.height)
        {
            if (tempSource != null)
            {
                tempSource.Release();
            }
            tempSource = new RenderTexture(source.width, source.height,
              source.depth);
            tempSource.Create();
        }

        // copy source pixels
        Graphics.Blit(source, tempSource);

        // do we need to create a new temporary destination render texture?
        if (tempDestination == null || source.width != tempDestination.width
           || source.height != tempDestination.height)
        {
            if (tempDestination != null)
            {
                tempDestination.Release();
            }
            tempDestination = new RenderTexture(source.width, source.height,
               source.depth);
            tempDestination.enableRandomWrite = true;
            tempDestination.Create();
        }

        // call the compute shader
        shader.SetInt("width", tempDestination.width);
        shader.SetInt("height", tempDestination.height);
        shader.SetInt("skipWidth", skipWidth);
        shader.SetFloat("thickness", lineThickness);
        shader.SetFloat("length", lineLength);
        shader.SetTexture(handleSobel, "source", tempSource);
        shader.SetTexture(handleSobel, "destination", tempDestination);
        shader.SetBuffer(handleSobel, "vertices", vertexBuffer);
        shader.Dispatch(handleSobel, Mathf.CeilToInt(tempDestination.width / 30.0f),
           Mathf.CeilToInt(tempDestination.height / 30.0f), 1);
        // copy the result
        Graphics.Blit(tempDestination, destination);
    }

    void OnPostRender()
    {
        Vertex[] vertices = new Vertex[6];
        vertices[0] = new Vertex { pos = new Vector2(0, 0), color = Color.green };
        vertices[1] = new Vertex { pos = new Vector2(0, 1), color = Color.green };
        vertices[2] = new Vertex { pos = new Vector2(1, 0), color = Color.green };
        vertices[3] = new Vertex { pos = new Vector2(1, 0), color = Color.green };
        vertices[4] = new Vertex { pos = new Vector2(0, 1), color = Color.green };
        vertices[5] = new Vertex { pos = new Vector2(1, 1), color = Color.green };

        vertexBuffer.SetData(vertices);
        Debug.Log(vertices[0].pos + "," + vertices[1].pos + "," + vertices[2].pos + "," + vertices[3].pos + "," + vertices[4].pos + "," + vertices[5].pos);
        drawLinesMaterial.SetBuffer("vertices", vertexBuffer);
        drawLinesMaterial.SetPass(0);
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));
        Graphics.DrawProcedural(drawLinesMaterial, bounds, MeshTopology.Triangles, 6, 1);
    }
}