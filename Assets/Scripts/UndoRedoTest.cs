using UnityEngine;
using UndoMethods;

public class UndoRedoTest : MonoBehaviour
{
    public GameObject Cube;
    private Color _Color;

    private void Start()
    {
        var transaction = new UndoRedoTransaction();
        SetColor(Color.red);
        SetColor(Color.green);
        SetColor(Color.blue);

        var top = UndoRedoManager.Instance.GetUndoStackTop<Color>();
        Debug.LogError(top.ToString());

        // CreateCube(Cube);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UndoRedoManager.Instance.Redo();
        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            UndoRedoManager.Instance.Undo();
        }
    }

    private void SetColor(Color color)
    {
        // 存储上一次Cube颜色
        UndoRedoManager.Instance.Push(SetColor, GetComponent<Renderer>().material.color, "新增颜色");
        GetComponent<Renderer>().material.color = color;
    }

    private void CreateCube(GameObject preCube)
    {
        var newCube = Instantiate(preCube);
        newCube.name = "This is a New Cube";
        newCube.transform.position = newCube.transform.position - Vector3.left;
        UndoRedoManager.Instance.Push(DestroyCube, newCube, "Create Cube");
    }

    private void DestroyCube(GameObject cube)
    {
        Destroy(cube);
        UndoRedoManager.Instance.Push(CreateCube, Cube, "Destroy Cube");
    }

    private void OnGUI()
    {
        var captionStyle = new GUIStyle
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            normal = {textColor = Color.white}
        };

        var tipStyle = new GUIStyle
        {
            fontSize = 18,
            fontStyle = FontStyle.Normal,
            normal = {textColor = Color.white}
        };

        GUI.Label(new Rect(10, 10, 200, 80), "Runtime Undo/Redo Example", captionStyle);
        GUI.Label(new Rect(Screen.width - 320, Screen.height - 30, 320, 30),
            "Press <color=#00ff00>U</color> to Undo, and press <color=#00ff00>R</color> to Redo.",
            tipStyle);
    }
}
