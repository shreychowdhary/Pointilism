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

    public bool drawLines;

    public bool blur;

    private struct Vertex
    {
        public Vector3 pos;
        public Vector4 color;
    }
    private Material drawLinesMaterial;

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
        vertexBuffer = new ComputeBuffer(6*958*538/skipWidth, sizeof(float) * (3 + 4));
        argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[4] { 6, 958 * 538 / skipWidth, 0, 0 });
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
        shader.SetBool("blur", blur);
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

        drawLinesMaterial.SetBuffer("vertices", vertexBuffer);
        drawLinesMaterial.SetPass(0);
        if (drawLines)
        {
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, argsBuffer);
        }
    }

}