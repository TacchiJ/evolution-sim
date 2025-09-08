using System;
using System.Collections.Generic;


public class NeuralNetwork
{
    // Layer sizes
    private int visionInputSize = 1085;
    private int hungerThirstSize = 2;

    private int visionHidden1 = 64;
    private int visionHidden2 = 32;
    private int hungerHidden = 8;
    private int mergedHidden = 16;
    private int outputSize = 2;

    // Weights and biases
    private float[,] W_v1, W_v2, W_h, W_merge, W_out;
    private float[] b_v1, b_v2, b_h, b_merge, b_out;

    private Random rnd = new Random();

    public NeuralNetwork()
    {
        // Xavier initialization
        W_v1 = InitWeights(visionInputSize, visionHidden1);
        b_v1 = InitBias(visionHidden1);

        W_v2 = InitWeights(visionHidden1, visionHidden2);
        b_v2 = InitBias(visionHidden2);

        W_h = InitWeights(hungerThirstSize, hungerHidden);
        b_h = InitBias(hungerHidden);

        int mergedSize = visionHidden2 + hungerHidden;
        W_merge = InitWeights(mergedSize, mergedHidden);
        b_merge = InitBias(mergedHidden);

        W_out = InitWeights(mergedHidden, outputSize);
        b_out = InitBias(outputSize);
    }

    // Forward pass
    public float[] Forward(float[] visionInput, float[] hungerThirstInput)
    {
        // Vision stream
        float[] v1 = LeakyReLU(Add(MatMul(visionInput, W_v1), b_v1));
        float[] v2 = LeakyReLU(Add(MatMul(v1, W_v2), b_v2));

        // Hunger/Thirst stream
        float[] h = LeakyReLU(Add(MatMul(hungerThirstInput, W_h), b_h));

        // Merge
        float[] merged = new float[v2.Length + h.Length];
        Array.Copy(v2, 0, merged, 0, v2.Length);
        Array.Copy(h, 0, merged, v2.Length, h.Length);

        float[] mergedOut = LeakyReLU(Add(MatMul(merged, W_merge), b_merge));

        // Output
        float[] output = Tanh(Add(MatMul(mergedOut, W_out), b_out));

        return output; // horizontal and vertical movement [-1,1]
    }

    #region MathHelpers
    private float[,] InitWeights(int inSize, int outSize)
    {
        float[,] w = new float[inSize, outSize];
        float std = (float)Math.Sqrt(2.0 / inSize); // Xavier for ReLU
        for (int i = 0; i < inSize; i++)
            for (int j = 0; j < outSize; j++)
                w[i, j] = (float)(NextGaussian() * std);
        return w;
    }

    private float[] InitBias(int size)
    {
        float[] b = new float[size];
        for (int i = 0; i < size; i++)
            b[i] = 0;
        return b;
    }

    private float[] MatMul(float[] input, float[,] weights)
    {
        int inSize = input.Length;
        int outSize = weights.GetLength(1);
        float[] output = new float[outSize];
        for (int j = 0; j < outSize; j++)
        {
            float sum = 0;
            for (int i = 0; i < inSize; i++)
            {
                sum += input[i] * weights[i, j];
            }
            output[j] = sum;
        }
        return output;
    }

    private float[] Add(float[] a, float[] b)
    {
        float[] c = new float[a.Length];
        for (int i = 0; i < a.Length; i++) c[i] = a[i] + b[i];
        return c;
    }

    private float[] ReLU(float[] x)
    {
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = Math.Max(0, x[i]);
        return y;
    }

    private float[] LeakyReLU(float[] x, float alpha = 0.01f)
    {
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++)
            y[i] = x[i] > 0 ? x[i] : alpha * x[i];
        return y;
    }

    private float[] Tanh(float[] x)
    {
        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = (float)Math.Tanh(x[i]);
        return y;
    }

    // Gaussian random generator
    private double NextGaussian()
    {
        double u1 = 1.0 - rnd.NextDouble(); 
        double u2 = 1.0 - rnd.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
    #endregion

    // --- Weight management for evolution ---
    public float[] GetWeights()
    {
        List<float> weights = new List<float>();

        // Flatten each weight matrix and bias vector
        Flatten(W_v1, weights); weights.AddRange(b_v1);
        Flatten(W_v2, weights); weights.AddRange(b_v2);
        Flatten(W_h, weights);  weights.AddRange(b_h);
        Flatten(W_merge, weights); weights.AddRange(b_merge);
        Flatten(W_out, weights); weights.AddRange(b_out);

        return weights.ToArray();
    }

    public void SetWeights(float[] flatWeights)
    {
        int index = 0;

        index = Unflatten(W_v1, b_v1, flatWeights, index);
        index = Unflatten(W_v2, b_v2, flatWeights, index);
        index = Unflatten(W_h, b_h, flatWeights, index);
        index = Unflatten(W_merge, b_merge, flatWeights, index);
        index = Unflatten(W_out, b_out, flatWeights, index);
    }

    // --- Helpers ---
    private void Flatten(float[,] matrix, List<float> list)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                list.Add(matrix[i, j]);
    }

    private int Unflatten(float[,] matrix, float[] bias, float[] flat, int index)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                matrix[i, j] = flat[index++];

        for (int i = 0; i < bias.Length; i++)
            bias[i] = flat[index++];

        return index;
    }

}
