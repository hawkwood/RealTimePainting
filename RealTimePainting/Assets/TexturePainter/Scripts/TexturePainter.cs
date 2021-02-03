/// <summary>
/// CodeArtist.mx 2018
/// This is the main class of the project, its in charge of raycasting to a model and place brush prefabs infront of the canvas camera.
/// If you are interested in saving the painted texture you can use the method at the end and should save it to a file.
/// </summary>


using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum Painter_BrushMode{PAINT = 0, DECAL, ERASE};

public class TexturePainter : MonoBehaviour
{
	public static TexturePainter Instance; //Our singleton instance

	[SerializeField]
	private ColorSelector colorSelector;
	[SerializeField]
	private GameObject brushCursor;
	[SerializeField]
	private GameObject brushContainer; //The cursor that overlaps the model and our container for the brushes painted
	[SerializeField]
	private Camera sceneCamera;
	[SerializeField]
	private Camera canvasCam;  //The camera that looks at the model, and the camera that looks at the canvas.    
	[SerializeField]
	private Sprite cursorDecal; // Cursor for the differen functions 
	[SerializeField]
	private Sprite spriteBrush;
	[SerializeField]
	private Sprite spriteCursor;
	[SerializeField]
	private RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes
	[SerializeField]
	private MeshRenderer canvasRenderer;

	private const int MAX_BRUSH_COUNT = 10000; //To avoid having millions of brushes

	private Texture2D cleanTexture;

	private Painter_BrushMode mode; //Our painter mode (Paint brushes or decals)
	private Color brushColor; //The selected color
	private float brushSize = 1.0f; //The size of our brush    
	private bool saving = false; //Flag to check if we are saving the texture
	private int brushCounter = 0;

	private const string texturePathKey = "SavedTexture";

	void Start()
    {
        Instance = this;

        cleanTexture = (Texture2D)canvasRenderer.material.mainTexture;

        LoadLastSavedTexture();
    }

	void Update()
	{
		brushColor = colorSelector.GetColor();  //Updates our painted color with the selected color
		if (Input.GetMouseButton(0))
		{
			DoAction();
		}
		UpdateBrushCursor();
	}

	//The main action, instantiates a brush or decal entity at the clicked position on the UV map
	void DoAction()
	{
		if (saving)
		{
			return;
		}

		Vector3 uvWorldPosition = Vector3.zero;
		if (HitTestUVPosition(ref uvWorldPosition))
		{
			GameObject brushObj;
			if (mode == Painter_BrushMode.PAINT)
			{
				brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a brush
				SpriteRenderer brushSpriteRenderer = brushObj.GetComponent<SpriteRenderer>();
				brushSpriteRenderer.color = brushColor; //Set the brush color
				brushSpriteRenderer.sprite = spriteBrush;//Set the brush sprite
			}
			else if (mode == Painter_BrushMode.ERASE)
			{
				brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a brush
				SpriteRenderer brushSpriteRenderer = brushObj.GetComponent<SpriteRenderer>();
				brushSpriteRenderer.color = Color.white; // brushColor; //Set the brush color
				brushSpriteRenderer.sprite = spriteBrush;//Set the brush sprite
			}
			else
			{
				brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/DecalEntity")); //Paint a decal
			}
			brushColor.a = brushSize * 2.0f; // Brushes have alpha to have a merging effect when painted over.
			brushObj.transform.parent = brushContainer.transform; //Add the brush to our container to be wiped later
			brushObj.transform.localPosition = uvWorldPosition; //The position of the brush (in the UVMap)
			brushObj.transform.localScale = Vector3.one * brushSize;//The size of the brush

			brushCounter++; //Add to the max brushes

			if (brushCounter >= MAX_BRUSH_COUNT) //If we reach the max brushes available, flatten the texture and clear the brushes
			{
				SaveTexture();
			}
		}
	}

    //To update at realtime the painting cursor on the mesh
    void UpdateBrushCursor()
	{
		Vector3 uvWorldPosition = Vector3.zero;
		if (HitTestUVPosition(ref uvWorldPosition) && !saving)
		{
			brushCursor.SetActive(true);
			brushCursor.transform.position = uvWorldPosition + brushContainer.transform.position;
		}
		else
		{
			brushCursor.SetActive(false);
		}
	}

	//Returns the position on the texuremap according to a hit in the mesh collider
	bool HitTestUVPosition(ref Vector3 uvWorldPosition)
	{
		RaycastHit hit;
		Vector3 cursorPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f);
		Ray cursorRay = sceneCamera.ScreenPointToRay(cursorPos);
		if (Physics.Raycast(cursorRay, out hit, 200))
		{
			MeshCollider meshCollider = hit.collider as MeshCollider;
			if (meshCollider == null || meshCollider.sharedMesh == null)
				return false;
			Vector2 pixelUV = new Vector2(hit.textureCoord.x, hit.textureCoord.y);
			uvWorldPosition.x = pixelUV.x - canvasCam.orthographicSize;//To center the UV on X
			uvWorldPosition.y = pixelUV.y - canvasCam.orthographicSize;//To center the UV on Y
			uvWorldPosition.z = 0.0f;
			return true;
		}
		else
		{
			return false;
		}
	}

	//Sets the base material with a our canvas texture, then removes all our brushes
	void ActuallySaveTexture()
	{
		// System.DateTime date = System.DateTime.Now;
		RenderTexture prevRT = RenderTexture.active;
		RenderTexture.active = canvasTexture;
		Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGB24, false);
		tex.ReadPixels(new Rect(0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
		tex.Apply();
		RenderTexture.active = prevRT;

		StartCoroutine (SaveTextureToFile(tex)); //Do you want to save the texture? This is your method!
	}

	////////////////// PUBLIC METHODS //////////////////

    public void ResetPainting()
    {
		foreach (Transform child in brushContainer.transform)
		{//Clear brushes
			Destroy(child.gameObject);
		}
		brushCounter = 0;
		canvasRenderer.material.mainTexture = cleanTexture;
	}

	public void SetBrushMode(Painter_BrushMode brushMode) //Sets if we are painting or placing decals
	{
		mode = brushMode;
		brushCursor.GetComponent<SpriteRenderer>().sprite = brushMode == Painter_BrushMode.DECAL ? cursorDecal : spriteCursor;
	}
	public void SetBrushSize(float newBrushSize)//Sets the size of the cursor brush or decal
	{
		brushSize = newBrushSize;
		brushCursor.transform.localScale = Vector3.one * brushSize;
	}

	public void SetNewBrush(Sprite newBrush, Sprite newCursor)
	{
		spriteBrush = newBrush;
		spriteCursor = newCursor;
		brushCursor.GetComponent<SpriteRenderer>().sprite = spriteCursor;
	}

    #region Load and Save

    public void LoadLastSavedTexture()
	{
		string savedTexturePath = PlayerPrefs.GetString(texturePathKey);
		if (!string.IsNullOrEmpty(savedTexturePath))
		{
			StartCoroutine(LoadTexture(savedTexturePath));
		}
	}

	IEnumerator LoadTexture(string path)
	{
		// TODO: check if path contains Assets (in Editor), or Resources, etc
		var www = UnityWebRequestTexture.GetTexture("file://" + path);
		yield return www.SendWebRequest();
		Texture2D tex = DownloadHandlerTexture.GetContent(www);
		if (tex != null)
		{
			canvasRenderer.material.mainTexture = tex;
		}
        else
        {
			Debug.LogWarning("Failed to load texture at: " + path);
        }
	}

	public void SaveTexture()
	{
		brushCursor.SetActive(false);
		saving = true;
		Invoke(nameof(this.ActuallySaveTexture), 0.1f);
	}

	IEnumerator SaveTextureToFile(Texture2D savedTexture)
	{
		var pngData = savedTexture.EncodeToPNG();
		if (pngData == null)
        {
			Debug.LogError("Saved texture has no data");
			SaveComplete(null);
			yield break;
        }

		brushCounter = 0;
		string path = "";
#if UNITY_EDITOR
		path = EditorUtility.SaveFilePanel("Save PNG", Application.dataPath, "PaintedTexture", "png");

		if (path.Length == 0)
        {
			Debug.Log("Save cancelled by user");
			SaveComplete(null);
			yield break;
		}
#else
		System.DateTime date = System.DateTime.Now;
		string fileName = "PaintedTexture " + date.ToShortDateString() + " " + date.ToShortTimeString() + ".png";
		fileName = fileName.Replace("/", "-");
		string dirPath = Path.Combine(Application.persistentDataPath, "SavedTextures");
		Directory.CreateDirectory(dirPath);

		path = Path.Combine(dirPath, fileName);
#endif


		File.WriteAllBytes(path, pngData);
		
		Debug.Log("<color=orange>Saved Successfully!</color> " + path);

		PlayerPrefs.SetString(texturePathKey, path);

		SaveComplete(savedTexture);

		yield return null;
	}

	//Show again the user cursor (To avoid saving it to the texture)
	void SaveComplete(Texture2D tex)
	{
		if (tex != null)
		{
			ResetPainting();
			canvasRenderer.material.mainTexture = tex;
		}

		brushCursor.SetActive(true);
		saving = false;
	}
    #endregion
}
