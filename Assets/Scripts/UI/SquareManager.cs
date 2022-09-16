using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A class for storing a single square
class Square
{
    // Colors for squares
    public static Color NoHighlight = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public static Color RedHighlight = new Color(0.8f, 0.5f, 0.6f, 1.0f);
    public static Color OrangeHighlight = new Color(0.8f, 0.5f, 0.4f, 1.0f);
    public static Color BlueHighlight = new Color(0.7f, 0.9f, 1.0f, 1.0f);

    // Variables for the square
    private SquareManager Manager;
    private bool SquareIsLightSquare;
    private GameObject SquareObject;
    private Transform SquareTransform;
    private Image ImageComponent;
    private Material DefaultSquareMaterial;

    private int SquareFile;
    private int SquareRank;

    // Create the square transform
    private Transform ConfigureSquareTransform(Transform squareParent)
    {
        // Create the transform
        SquareTransform = SquareObject.transform;
        SquareTransform.SetParent(squareParent);

        // Position, scale, etc.
        Vector3 SquarePosition = new Vector3(-43.75f + SquareFile * 12.5f, -43.75f + SquareRank * 12.5f, 0);
        Vector3 SquareScale = new Vector3(0.125f, 0.125f, 0);

        SquareTransform.localPosition = SquarePosition;
        SquareTransform.localScale = SquareScale;

        // Return the transform
        return SquareTransform;
    }

    // Add components to the square object
    private void PopulateComponents()
    {
        // Add a canvas renderer and an image
        SquareObject.AddComponent<CanvasRenderer>();
        ImageComponent = SquareObject.AddComponent<Image>();
    }

    // Main constructor
    public Square(SquareManager squareManager, Transform parent, int file, int rank, string name, bool isLightSquare)
    {
        // Setup the square object
        Manager = squareManager;
        SquareFile = file;
        SquareRank = rank;
        SquareIsLightSquare = isLightSquare;
        SquareObject = new GameObject(name, typeof(RectTransform));

        // Setup the transform and add components
        SquareTransform = ConfigureSquareTransform(parent);
        PopulateComponents();
    }

    // Revert to the default image material
    public void RevertImageMaterial()
    {
        SetImageMaterial(DefaultSquareMaterial);
    }

    // Set the default image material
    public void SetDefaultImageMaterial(Material newDefaultMaterial)
    {
        DefaultSquareMaterial = newDefaultMaterial;
        SetImageMaterial(DefaultSquareMaterial);
    }

    // Set the material
    public void SetImageMaterial(Material newMaterial)
    {
        ImageComponent.material = newMaterial;
    }

    // Set the highlight of the square
    public void SetHighlight(Color highlight)
    {
        ImageComponent.color = highlight;
    }

    // Get the square color
    public bool GetIsLightSquare()
    {
        return SquareIsLightSquare;
    }

    // Get the square transform
    public Transform GetTransform()
    {
        return SquareTransform;
    }

    // Get the default image material
    public Material GetDefaultImageMaterial()
    {
        return DefaultSquareMaterial;
    }

    // Get the current image material
    public Material GetImageMaterial()
    {
        return ImageComponent.material;
    }
}

// The piece renderer handles drawing the pieces onto the board
class PieceRenderer
{
    // Variables for the piece renderer
    private SquareManager Manager;
    private GameObject PieceObject;
    private Transform PieceTransform;
    private Image ImageComponent;
    private Material CurrentImage;

    // Create the square transform
    private Transform ConfigurePieceRendererTransform(Transform pieceRendererParent)
    {
        // Create the transform
        PieceTransform = PieceObject.transform;
        PieceTransform.SetParent(pieceRendererParent);

        // Position, scale, etc.
        Vector3 PieceRendererPosition = new Vector3(-43.75f, -43.75f, 0);
        Vector3 PieceRendererScale = new Vector3(0.125f, 0.125f, 0);

        PieceTransform.localPosition = PieceRendererPosition;
        PieceTransform.localScale = PieceRendererScale;

        // Return the transform
        return PieceTransform;
    }

    // Add components to the piece renderer object
    private void PopulateComponents()
    {
        // Add a canvas renderer and an image
        PieceObject.AddComponent<CanvasRenderer>();
        ImageComponent = PieceObject.AddComponent<Image>();

        // Set the image for the image component
        SetImage(CurrentImage);
    }

    // Main constructor
    public PieceRenderer(SquareManager squareManager, Transform parent, string name, Material startingPieceType)
    {
        // Setup class variables
        CurrentImage = startingPieceType;

        // Setup the piece renderer object
        Manager = squareManager;
        PieceObject = new GameObject(name, typeof(RectTransform));

        // Setup the transform and add components
        PieceTransform = ConfigurePieceRendererTransform(parent);
        PopulateComponents();
    }

    // Enable the image component
    public void EnableImageComponent()
    {
        ImageComponent.enabled = true;
    }

    // Disable the image component (i.e. the piece is captured)
    public void DisableImageComponent()
    {
        ImageComponent.enabled = false;
    }

    // Set the image in the image component
    public void SetImage(Material newMaterial)
    {
        ImageComponent.material = newMaterial;
        CurrentImage = newMaterial;
    }

    // Set the square position for the image
    public void SetSquarePosition(int index)
    {
        int[] fileAndRank = Board.SquareIndexToFileAndRank(index);
        Vector3 newPosition = new Vector3(-43.75f + fileAndRank[0] * 12.5f, -43.75f + fileAndRank[1] * 12.5f, 0);
        PieceTransform.localPosition = newPosition;
    }

    // Set the square position for the image by file and rank as floats
    public void SetSquarePosition(float file, float rank)
    {
        Vector3 newPosition = new Vector3(-43.75f + file * 12.5f, -43.75f + rank * 12.5f, 0);
        PieceTransform.localPosition = newPosition;
    }

    // Get the name of the piece renderer
    public string GetName()
    {
        return PieceObject.name;
    }
}

// The square manager can change aspects of many squares
public class SquareManager : MonoBehaviour
{

    // Class variables
    private Square[] squares;

    // Materials for the squares
    [Header("Square Materials")] // Text Title of "Square Materials"
    public Material DefaultLightSquareMaterial;
    public Material DefaultDarkSquareMaterial;

    // Textures for the images of the pieces
    [Space(10)] // 10 pixels of spacing here.
    [Header("Piece Textures")] // Text Title of "Piece Textures"
    public Material Material2DWhitePawn;
    public Material Material2DWhiteKnight;
    public Material Material2DWhiteBishop;
    public Material Material2DWhiteRook;
    public Material Material2DWhiteQueen;
    public Material Material2DWhiteKing;
    public Material Material2DBlackPawn;
    public Material Material2DBlackKnight;
    public Material Material2DBlackBishop;
    public Material Material2DBlackRook;
    public Material Material2DBlackQueen;
    public Material Material2DBlackKing;
    private Dictionary<int, Material> PieceToMaterial;

    // A list of piece renderers
    private List<PieceRenderer> AllPieceRenderers;

    // For piece dragging purposes
    private int PieceDragOrigin;
    public const int DragPieceRenderer = 64;

    // Create the squares
    private void CreateSquares()
    {
        // Create all the squares starting from h8 and going down to a1
        squares = new Square[64];
        for (int file = 7; file >= 0; file--)
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                bool lightSquare = (file + rank) % 2 == 1;
                squares[file + rank * 8] = new Square(this, transform, file, rank, $"{Board.FileNames[file]}{(rank + 1)}", lightSquare);
                squares[file + rank * 8].SetDefaultImageMaterial(lightSquare ? DefaultLightSquareMaterial : DefaultDarkSquareMaterial);
            }
        }
    }

    // Create the piece renderers
    private void CreatePieceRenderers()
    {
        // Initialize the list
        AllPieceRenderers = new List<PieceRenderer>();

        // Create 64 piece renderers for up to 64 pieces
        // A regular game can only have 32 but fun positions might have more
        for (int i = 0; i < 65; i++)
        {
            AllPieceRenderers.Add(new PieceRenderer(this, transform, $"Renderer#{i+1}", Material2DWhitePawn));
            AllPieceRenderers[i].SetSquarePosition(i);
            AllPieceRenderers[i].DisableImageComponent();
        }
    }

    // Create the dictionary of piece bits to textures
    private void CreatePieceToTextureDictionary()
    {
        // Initialize the dictionary
        PieceToMaterial = new Dictionary<int, Material>();

        // Populate it
        PieceToMaterial[Piece.White | Piece.Pawn] = Material2DWhitePawn;
        PieceToMaterial[Piece.White | Piece.Knight] = Material2DWhiteKnight;
        PieceToMaterial[Piece.White | Piece.Bishop] = Material2DWhiteBishop;
        PieceToMaterial[Piece.White | Piece.Rook] = Material2DWhiteRook;
        PieceToMaterial[Piece.White | Piece.Queen] = Material2DWhiteQueen;
        PieceToMaterial[Piece.White | Piece.King] = Material2DWhiteKing;

        PieceToMaterial[Piece.Black | Piece.Pawn] = Material2DBlackPawn;
        PieceToMaterial[Piece.Black | Piece.Knight] = Material2DBlackKnight;
        PieceToMaterial[Piece.Black | Piece.Bishop] = Material2DBlackBishop;
        PieceToMaterial[Piece.Black | Piece.Rook] = Material2DBlackRook;
        PieceToMaterial[Piece.Black | Piece.Queen] = Material2DBlackQueen;
        PieceToMaterial[Piece.Black | Piece.King] = Material2DBlackKing;
    }

    // Initialize all of the class values
    public void Initialize()
    {
        // The piece drag initializes to -1
        PieceDragOrigin = -1;

        // Initialize class variables
        CreatePieceToTextureDictionary();

        // Populate the board with squares and piece renderers
        CreateSquares();
        CreatePieceRenderers();
    }

    // Turn a bit representation of a piece into its material
    public Material GetPieceTexture(int piece)
    {
        if (Piece.Type(piece) == Piece.None)
        {
            return null;
        }
        return PieceToMaterial[piece];
    }

    // Change the highlight of a square
    public void ChangeSquareHighlight(int square, Color newHighlight)
    {
        squares[square].SetHighlight(newHighlight);
    }

    // Change the highlight of all squares
    public void ChangeAllSquareHighlights(Color newHighlight)
    {
        for (int i = 0; i < 64; i++)
        {
            squares[i].SetHighlight(newHighlight);
        }
    }

    // Update the piece renderers to match the current board position
    public void UpdatePieceImagesByBoard(Board board)
    {
        // Go through the board spaces
        for (int i = 0; i < 64; i++)
        {
            // Otherwise, render whatever is on the square
            if (i != PieceDragOrigin && Piece.Type(board.spaces[i]) != Piece.None)
            {
                AllPieceRenderers[i].SetImage(GetPieceTexture(board.spaces[i]));
                AllPieceRenderers[i].EnableImageComponent();
            }
            // Disable all of the other renderers
            else
            {
                AllPieceRenderers[i].DisableImageComponent();
            }
        }
    }

    // Change the position of a piece renderer
    public void SetPieceRendererSquarePosition(int index, float file, float rank)
    {
        AllPieceRenderers[index].SetSquarePosition(file, rank);
    }

    // Change the image of a piece renderer
    public void SetPieceRendererImage(int index, Material newImage)
    {
        AllPieceRenderers[index].SetImage(newImage);
    }

    // Set the piece drag origin
    public void SetPieceDragOrigin(int newOrigin)
    {
        // Reset the origin
        if (PieceDragOrigin >= 0)
        {
            AllPieceRenderers[PieceDragOrigin].EnableImageComponent();
            AllPieceRenderers[DragPieceRenderer].DisableImageComponent();
        }

        // Setup the new origin
        PieceDragOrigin = newOrigin;
        if (newOrigin > 0)
        {
            AllPieceRenderers[PieceDragOrigin].DisableImageComponent();
            AllPieceRenderers[DragPieceRenderer].EnableImageComponent();
        }
    }
}