using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadManager : MonoBehaviour
{
	private VrPlayer _player;

	public void Initialize(VrPlayer player)
	{
		_player = player;
	}
}
