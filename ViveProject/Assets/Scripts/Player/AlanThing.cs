using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlanThing : MonoBehaviour {

	GridTile[,] gridTiles = new GridTile[4, 4];

	/*
	 * 
	0, 0, 0, 0
	0, 0, p, 0
	0, 0, 0, 0
	0, 0, 0, 0

	*/

	void Start () {
		int sizeOfGame = 6;
		gridTiles = new GridTile[sizeOfGame, sizeOfGame];

		gridTiles[0, 0].typeOfTile = "";

		Vector2 piecePosition = new Vector2(2, 2);
		// piece1.transform.position = new Vector3(2, 2)
		if (gridTiles[(int)piecePosition.x - 1, (int)piecePosition.y].typeOfTile == "Empty") {

		}
	}

}
