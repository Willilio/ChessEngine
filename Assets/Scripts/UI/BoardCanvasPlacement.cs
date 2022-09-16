using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An enum which describes whether the board is placed with measurements of the
// x axis or the y axis
public enum BoardSizingAxis
{
    X_AXIS,
    Y_AXIS,
}

// The placement tool for the board
public class BoardCanvasPlacement : MonoBehaviour
{

    // Remember the dimensions of the screen in pixels
    private int PixelScreenSizeX, PixelScreenSizeY;

    // Board placement parameters
    private float PixelXMargin = 20;
    private float PixelYMargin = 20;

    // Board relative positioning information
    [HideInInspector] public BoardSizingAxis BoardSizingMode;
    [HideInInspector] public float TLX, TLY; // TL = top left
    [HideInInspector] public float BRX, BRY; // BR = bottom right

    // Update the pixel screen sizes
    private void UpdatePixelScreenSize()
    {
        PixelScreenSizeX = Screen.width;
        PixelScreenSizeY = Screen.height;
    }

    // Recalculate the bounds of the board
    private void RecalculateBoardBounds()
    {
        switch (BoardSizingMode)
        {
            case BoardSizingAxis.Y_AXIS:

                // Y axis placement
                TLX = 0.75f - (PixelScreenSizeY - 40f) / (2f * PixelScreenSizeX);
                TLY = 1 - 20f / PixelScreenSizeY;
                BRX = 0.75f + (PixelScreenSizeY - 40f) / (2f * PixelScreenSizeX);
                BRY = 20f / PixelScreenSizeY;
                break;

            case BoardSizingAxis.X_AXIS:

                // X axis placement
                TLX = 0.5f + 20f / PixelScreenSizeX;
                TLY = 0.5f + (PixelScreenSizeX - 80f) / (4f * PixelScreenSizeY);
                BRX = 1f - (20f / PixelScreenSizeX);
                BRY = 0.5f - (PixelScreenSizeX - 80f) / (4f * PixelScreenSizeY);
                break;
        }
    }

    // Update the board transform
    private void UpdateBoardTransform()
    {
        // Use margins and keep the board on the right half of the screen
        float BoardSideLengthXAxis = (PixelScreenSizeX / 2) - (PixelXMargin * 2);
        float BoardSideLengthYAxis = PixelScreenSizeY - (PixelYMargin * 2);
        BoardSizingMode = BoardSideLengthXAxis > BoardSideLengthYAxis ? BoardSizingAxis.Y_AXIS : BoardSizingAxis.X_AXIS;
        float BoardSideLength = Mathf.Min(BoardSideLengthXAxis, BoardSideLengthYAxis);
        Vector3 BoardCenterPositionOffset = new Vector3(PixelScreenSizeX / 4, 0, 0);
        Vector3 BoardScale = new Vector3(BoardSideLength / 100, BoardSideLength / 100, 0);

        // Set the transform to the new parameters
        transform.localPosition = BoardCenterPositionOffset;
        transform.localScale = BoardScale;
    }

    // Resize the entire board
    private void ResizeBoard()
    {
        UpdatePixelScreenSize();
        UpdateBoardTransform();
        RecalculateBoardBounds();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Setup the variables for the class
        BoardSizingMode = BoardSizingAxis.X_AXIS;

        // Resize the board to calculate the rest of the values
        ResizeBoard();
    }

    // Update is called once per frame
    void Update()
    {
        // Just constantly resize the board
        // Probably an enormous source of lag
        ResizeBoard();
    }
}
