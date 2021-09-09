using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class ShooterNetManager : NetworkManager
{
	[Header("Game variables")] 
	[SerializeField] private Transform[] spawnPoints;
	
	public override void OnServerAddPlayer(NetworkConnection conn)
	{
		base.OnServerAddPlayer(conn);

		//conn.identity.transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
	}
}
