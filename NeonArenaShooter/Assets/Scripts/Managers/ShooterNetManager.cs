using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Random = UnityEngine.Random;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class ShooterNetManager : NetworkManager
{
	[SerializeField] private List<NetworkConnection> teamOne = new List<NetworkConnection>(), teamTwo = new List<NetworkConnection>();

	public override void OnStartHost()
	{
		base.OnStartHost();
		
	}

	public override void OnServerAddPlayer(NetworkConnection conn)
	{
		base.OnServerAddPlayer(conn);

		if (teamOne.Count == teamTwo.Count)
		{
			var rand = Random.Range(1, 3);
			ref List<NetworkConnection> team = ref teamOne;

			if (rand == 2)
				team = ref teamTwo;
			
			team.Add(conn);
			conn.identity.GetComponent<PlayerController>().SetTeam(rand);
		}
		else if (teamOne.Count > teamTwo.Count)
		{
			teamTwo.Add(conn);
			conn.identity.GetComponent<PlayerController>().SetTeam(2);
		}
		else
		{
			teamOne.Add(conn);
			conn.identity.GetComponent<PlayerController>().SetTeam(1);
		}
	}

	//Chamado no servidor, somente
	public override void OnServerDisconnect(NetworkConnection conn)
	{
		base.OnServerDisconnect(conn);

		teamOne.Remove(conn);
		teamTwo.Remove(conn);
	}
}
