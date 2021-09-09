using UnityEngine;

public class FpsEnabler : MonoBehaviour
{
    [SerializeField] private GameObject fpsModel, tpsModel;
    [SerializeField] private Transform fpsHand, tpsHand, gun;
    
    public void Switch(bool isFps)
    {
        if (isFps)
        {
            fpsModel.SetActive(true);
            tpsModel.SetActive(false);
            gun.SetParent(fpsHand);
        }
        else
        {
            fpsModel.SetActive(false);
            tpsModel.SetActive(true);
            gun.SetParent(tpsHand);
        }

        gun.localPosition = Vector3.zero;
        gun.localEulerAngles = Vector3.zero;
        gun.localScale = Vector3.one;
    }
}