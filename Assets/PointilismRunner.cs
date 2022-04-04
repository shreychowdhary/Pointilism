using System;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Camera))]

public class PointilismRunner : MonoBehaviour
{

    public ComputeShader sobelShader;
    public ComputeShader gaussianBlurShader;
    public ComputeShader pointilismShader;
    public Shader drawLineShader;

    public int skipWidth;

    public float size;
    public float lengthFactor;

    public float randomPositionFactor;

    public bool drawLines;


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
    private ComputeBuffer gradientBuffer;

    private int handleSobel;
    private int handleGaussianHorizontal;
    private int handleGaussianVertical;
    private int handlePointilism;


    void Start()
    {
        if (sobelShader == null || gaussianBlurShader == null || pointilismShader == null)
        {
            Debug.Log("Shader missing.");
            enabled = false;
            return;
        }

        handleSobel = sobelShader.FindKernel("Sobel");
        handleGaussianHorizontal = gaussianBlurShader.FindKernel("GaussianBlurHorizontal");
        handleGaussianVertical = gaussianBlurShader.FindKernel("GaussianBlurVertical");
        handlePointilism = pointilismShader.FindKernel("Pointilism");
        if (handleSobel < 0 || handleGaussianHorizontal < 0 || handleGaussianVertical < 0 || handlePointilism < 0)
        {
            Debug.Log("Initialization failed.");
            enabled = false;
            return;
        }

        drawLinesMaterial = new Material(drawLineShader);
        vertexBuffer = new ComputeBuffer(6 * 1920 * 1080, sizeof(float) * (3 + 4));
        gradientBuffer = new ComputeBuffer(1920 * 1080, sizeof(float) * 2);

        argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[4] { 6, 1920 * 1080 / skipWidth, 0, 0 });
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
        if (sobelShader == null || handleSobel < 0 || source == null)
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

        // Call Sobel Gradient kernel
        sobelShader.SetInt("width", tempDestination.width);
        sobelShader.SetInt("height", tempDestination.height);
        sobelShader.SetTexture(handleSobel, "source", tempSource);
        sobelShader.SetBuffer(handleSobel, "gradient", gradientBuffer);
        sobelShader.Dispatch(handleSobel, Mathf.CeilToInt(tempDestination.width / 30f),
           Mathf.CeilToInt(tempDestination.height / 30f), 1);


        // Call Horizontal Gaussian Blur kernel
        gaussianBlurShader.SetInt("width", tempDestination.width);
        gaussianBlurShader.SetInt("height", tempDestination.height);
        gaussianBlurShader.SetBuffer(handleGaussianHorizontal, "gradient", gradientBuffer);
        gaussianBlurShader.Dispatch(handleGaussianHorizontal, Mathf.CeilToInt(tempDestination.width / 512f),
           Mathf.CeilToInt(tempDestination.height / 2f), 1);
        // Vertical Pass
        gaussianBlurShader.SetBuffer(handleGaussianVertical, "gradient", gradientBuffer);
        gaussianBlurShader.Dispatch(handleGaussianVertical, Mathf.CeilToInt(tempDestination.width / 2f),
           Mathf.CeilToInt(tempDestination.height / 512f), 1);

        // Draw Lines
        pointilismShader.SetInt("width", tempDestination.width);
        pointilismShader.SetInt("height", tempDestination.height);
        pointilismShader.SetInt("skipWidth", skipWidth);
        pointilismShader.SetFloat("size", size);
        pointilismShader.SetFloat("lengthFactor", lengthFactor);
        pointilismShader.SetFloat("randomPositionFactor", randomPositionFactor);
        pointilismShader.SetBuffer(handlePointilism, "gradient", gradientBuffer);
        pointilismShader.SetBuffer(handlePointilism, "vertices", vertexBuffer);
        pointilismShader.SetTexture(handlePointilism, "source", tempSource);
        pointilismShader.SetTexture(handlePointilism, "destination", tempDestination);
        pointilismShader.SetTextureFromGlobal(handlePointilism, "depth", "_CameraDepthTexture");

        if (drawLines)
        {
            pointilismShader.Dispatch(handlePointilism, Mathf.CeilToInt(tempDestination.width / 32f),
               Mathf.CeilToInt(tempDestination.height / 32f), 1);
            Graphics.Blit(tempDestination, destination);
        }
        else
        {
            Graphics.Blit(tempSource, destination);
        }

        drawLinesMaterial.SetBuffer("vertices", vertexBuffer);
        drawLinesMaterial.SetPass(0);
        if (drawLines)
        {
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, argsBuffer);
        }
    }

}