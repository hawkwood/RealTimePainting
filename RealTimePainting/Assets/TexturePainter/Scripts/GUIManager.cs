using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class GUIManager : MonoBehaviour
{
	public Text guiTextMode;
	public Slider sizeSlider;
	public TexturePainter painter;

	public void SetBrushMode(int newMode)
	{
		Painter_BrushMode brushMode = Painter_BrushMode.PAINT + newMode;

		string colorText = brushMode == Painter_BrushMode.PAINT ? "orange" : "purple";

		guiTextMode.text = "<b>Mode:</b><color=" + colorText + ">" + brushMode.ToString() + "</color>";

		painter.SetBrushMode(brushMode);
	}

	public void UpdateSizeSlider()
	{
		painter.SetBrushSize(sizeSlider.value);
	}
}
