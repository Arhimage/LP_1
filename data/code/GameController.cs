using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "9825f12c1b49caaf96a33f1f36d4926585a63fd6")]
public class GameController : Component
{
	private enum Axis
	{
		X,
		Y,
		NX,
		NY,
	}

	private List<Node> _cagleList = new();

	private bool _gameStarted = false;

	[ShowInEditor]
	[Parameter (Title = "Игровой пол")]
	private Node _floor = null;

	[ShowInEditor]
	[Parameter (Title = "Шар для боулинга")]
	private Node _ball = null;

	[ShowInEditor]
	[Parameter (Title = "Расстояние пересечения")]
	private float _pokeDistance = 100.0f;

	[ShowInEditor]
	[ParameterMask (Title = "Маска пересчеения", MaskType = ParameterMaskAttribute.TYPE.INTERSECTION)]
	private int _mask = 0;

	[ShowInEditor]
	[ParameterFile (Title = "Образец кегли", Filter = ".node")]
	private string _cagleTemplate = null;

	[ShowInEditor]
	[Parameter (Title = "Точка появления")]
	private Node _spawnPoint = null;

	[ShowInEditor]
	[Parameter (Title = "Направление появления")]
	private Axis _axis = Axis.X;

	[ShowInEditor]
	[ParameterSlider (Title = "Количество", Group = "Параметры появления", Min = 1, Max = 100)]
	private int _quantity = 10;

	[ShowInEditor]
	[Parameter (Title = "Смещение при появлении", Group = "Параметры появления")]
	private vec3 _offset = new vec3(0.2, 0.2, 1.2);

	void Init()
	{
		Input.MouseHandle = Input.MOUSE_HANDLE.SOFT;
		if (_spawnPoint == null || _cagleTemplate == null || _floor == null || _ball == null)
		{
			Log.ErrorLine("Не установлен один из параметров!");
		}
		else
		{
			_ball = _ball.GetChild(0);
			SpawnCalges();
		}
		Visualizer.Enabled = true;
	}

	private void SpawnCalges()
	{
		int row = 0;
		int cellMax = 1;
		int cell = 0;
		for (int i = 0; i < _quantity; i++)
		{
			Node cagle = World.LoadNode(_cagleTemplate);
			cagle.WorldPosition = _spawnPoint.WorldPosition + GetCagleWorldPosition(row, cell);
			_cagleList.Add(cagle);
			Log.MessageLine(_cagleList.Count);
			cell++;
			if (cell >= cellMax)
			{
				row++;
				cell = 0;
				cellMax++;
			}
		}
	}

	private vec3 GetCagleWorldPosition(int row, int cell)
	{
		double width = - row * _offset.x + cell * _offset.x * 2;
		double height = - row * _offset.y;
		return _axis switch
		{
			Axis.X => new vec3(width, height, _offset.z),
			Axis.Y => new vec3(height, width, _offset.z),
			Axis.NX => new vec3(-width, -height, _offset.z),
			Axis.NY => new vec3(-height, -width, _offset.z),
			_ => throw new Exception("Не добавлено одно из направлений появления кегль!")
		};
	}
	
	void Update()
	{
		vec3 firstPoint = (vec3)Game.Player.WorldPosition;
		ivec2 mouse_coord = Input.MousePosition;
		vec3 secondPoint = firstPoint + Game.Player.GetDirectionFromMainWindow(mouse_coord.x, mouse_coord.y) * _pokeDistance;

		WorldIntersection worldIntersection= new WorldIntersection();
		Unigine.Object hitObject = World.GetIntersection(firstPoint, secondPoint, _mask, worldIntersection);
		if (hitObject != null)
		{
			Node ball = hitObject;
			if (Input.IsMouseButtonPressed(Input.MOUSE_BUTTON.LEFT) && !_gameStarted)
			{
				ball.ObjectBodyRigid.AddWorldImpulse(ball.WorldPosition, -(vec3)(worldIntersection.Point - ball.WorldPosition).Normalized * 30);
				_gameStarted = true;
			}
			Visualizer.RenderPoint3D(worldIntersection.Point, 0.01f, vec4.RED);
		}
		GameOver();
	}

	private void GameOver()
	{
		if (!_gameStarted)
		{
			return;
		}
		if (_ball.WorldPosition.z < -100 || _ball.ObjectBodyRigid.GetVelocity(vec3.ZERO) == 0)
		{
			Log.WarningLine($"Your result {GetResult()}!");
		}
	}

	int result = 0;
	private int GetResult()
	{
		for (int i = 0; i < _cagleList.Count; i++)
		{
			if (MathLib.Abs(_cagleList[i].GetWorldDirection(MathLib.AXIS.NZ).z) < 0.85)
			{
				_cagleList.RemoveAt(i);
				result++;
				i--;
			}
		}
		return result;
	}
}