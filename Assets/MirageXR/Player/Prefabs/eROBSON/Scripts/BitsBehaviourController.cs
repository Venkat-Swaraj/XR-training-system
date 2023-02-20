using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MirageXR;
using UnityEngine;
using Obi;

public class BitsBehaviourController : MonoBehaviour
{
    private eROBSONItems _erobsonItem;

    private void Awake()
    {
        _erobsonItem = GetComponent<eROBSONItems>();
    }


    private void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        ErobsonItemManager.OnBitConnected += OnItemConnected;
        ErobsonItemManager.OnBitDisconnected += OnItemDisconnected;
    }

    private void OnDisable()
    {
        ErobsonItemManager.OnBitConnected -= OnItemConnected;
        ErobsonItemManager.OnBitDisconnected -= OnItemDisconnected;
    }


    private bool CircuitControlling
    {
        get; set;
    }


    /// <summary>
    /// Initiate the bits at the start
    /// </summary>
    public void Init()
    {
        if (!_erobsonItem)
            return;


        _erobsonItem.IsActive = true;
        _erobsonItem.HasPower = false;
        _erobsonItem.Dimmable = false;

        switch (_erobsonItem.ID)
        {
            case BitID.O2LONGLED:
            case BitID.O6BUZZER:
            case BitID.O9BARGRAPH:
            case BitID.O13FAN:
            case BitID.P3USBPOWERCONNECTOR:
            case BitID.W2BRANCH:
            case BitID.W7FORK:
                break;
            case BitID.USBPOWER:
                _erobsonItem.HasPower = true;
                break;
            case BitID.I3BUTTON:
                _erobsonItem.IsActive = false;
                break;
            case BitID.I5SLIDEDIMMER:
                _erobsonItem.Dimmable = true;
                _erobsonItem.Value = 0.5f;
                break;
            case BitID.I11PRESSURESENSOR:
            case BitID.I18MOTIONSENSOR:
            case BitID.O25DCMOTOR:
                _erobsonItem.Dimmable = true;
                _erobsonItem.Value = 0.0f;
                break;
        }
    }



    /// <summary>
    /// Toggle the status of the bit
    /// </summary>
    public void ActivatingToggle()
    {
        _erobsonItem.IsActive = !_erobsonItem.IsActive;
        _erobsonItem.HasPower = _erobsonItem.IsActive;

        //Turn on/off the power indicators
        SwitchPowerIndicatorLight(_erobsonItem);

        ControlCircuit();
    }



    public void SetValue(float bitValue)
    {
        _erobsonItem.Value = bitValue;
    }



    /// <summary>
    /// Control every bit in this circuit and active/diactive it if it is not connected to power sourse 
    /// </summary>
    public async void ControlCircuit()
    {
        //try
        //{
            if (CircuitControlling)
                return;

            CircuitControlling = true;

            var eRobsonItemsList = ErobsonItemManager.ERobsonItemsList;

            //Order the list of bits by "connectedTime" variable
            eRobsonItemsList = eRobsonItemsList.OrderBy(e => e.connectedTime).ToList();


            //If the bit is a power source move it on top
            // Find index for bottom item
            var idx = eRobsonItemsList.FindIndex(i => i.ID == BitID.USBPOWER);

            // Move power source item to first if it is not already and it is connected to the circuit
            if (idx > 0 && ErobsonItemManager.ERobsonConnectedItemsList.Contains(eRobsonItemsList[idx]))
            {
                MoveToTop(eRobsonItemsList, idx);
            }


            //Check all bits and deactivate all bits after this deactivated bit
            foreach (var eRobsonItem in eRobsonItemsList)
            {
                if (!eRobsonItem)
                {
                    continue;
                }

                //Check the bits which are connected to this bit
                var hasConnectedPower = await HasConnectedPower(eRobsonItem);

                if (hasConnectedPower)
                {
                    if (eRobsonItem.IsActive || (eRobsonItem.Dimmable && eRobsonItem.Value > 0))
                    {
                        eRobsonItem.HasPower = true;
                        ErobsonItemManager.AddOrRemoveFromConnectedList(eRobsonItem, AddOrRemove.ADD);
                        BitActionToggle(eRobsonItem, true);
                    }
                }
                else
                {
                    eRobsonItem.HasPower = false;
                    ErobsonItemManager.AddOrRemoveFromConnectedList(eRobsonItem, AddOrRemove.REMOVE);
                    BitActionToggle(eRobsonItem, false);
                }

            }



            //set the value text
            _erobsonItem.SetValueText(_erobsonItem.ID);

            CircuitControlling = false;
    //}
    //    catch (Exception e)
    //    {

    //        CircuitControlling = false;
    //        Debug.LogError(e);
    //    }
    }


    /// <summary>
    /// Check if any dimmable bit exist in the curcuit within the given bit
    /// If exist return the average value otherwise return -1
    /// </summary>
    /// <returns>float</returns>
    private static float CalculateValue(eROBSONItems erobsonItem)
    {
        var eRobsonConnectedItemsList = ErobsonItemManager.ERobsonConnectedItemsList;
        var dimmablesToBeCaluculated = eRobsonConnectedItemsList.FindAll(b => b.Dimmable == true && eRobsonConnectedItemsList.IndexOf(b) < eRobsonConnectedItemsList.IndexOf(erobsonItem));

        if (dimmablesToBeCaluculated.Count == 0)
        {
            return -1;
        }


        float valueSum = 0;
        float counter = 0;
        foreach (var bit in dimmablesToBeCaluculated)
        {
            valueSum += bit.Value;
            counter++;
        }

        return valueSum / counter;
    }


    /// <summary>
    /// Controll if is power connected from the source to the given bit
    /// This will check all bits from the source to the given bit
    /// </summary>
    /// <param name="bit"></param>
    /// <returns></returns>
    private static async Task<bool> HasConnectedPower(eROBSONItems bit)
    {
        //Power source doesn't need to be check, it has power :)
        if (bit.ID == BitID.USBPOWER)
        {
            return true;
        }

        //Check the connected bit
        foreach (var connectedbit in bit.ConnectedBits)
        {
            //The connected bit has both actived and power
            if (connectedbit.HasPower && connectedbit.IsActive)
            {
                //Then check if it is power source return true
                if (connectedbit.ID == BitID.USBPOWER)
                {
                    return true;
                }
                //If it is not power source check all previous bits are connected or not 
                //Do not check the bit itself again
                if (connectedbit != bit)
                {
                    return await HasConnectedPower(connectedbit);
                }
            }
        }

        return false;
    }


    /// <summary>
    /// The behaviour of the bit after connecting
    /// </summary>
    private void OnItemConnected(eROBSONItems bit)
    {
        if (bit != _erobsonItem)
            return;

        ControlCircuit();
    }


    /// <summary>
    /// The behaviour of the bit after disconnecting
    /// </summary>
    private void OnItemDisconnected(eROBSONItems bit)
    {
        if (bit != _erobsonItem)
            return;

        ControlCircuit();
    }


    /// <summary>
    /// Set the value of sliders
    /// </summary>
    public async void SetValue()
    {
        var pinchSlider = GetComponentInChildren<PinchSlider>();
        if (pinchSlider)
        {
            _erobsonItem.Value = pinchSlider.SliderValue;

            ControlCircuit();

            await Task.Delay(100);
        }
    }


    /// <summary>
    /// Turn on or off the power indictor light depends on it has power(connected) or not
    /// </summary>
    /// <param name="bit"></param>
    private static void SwitchPowerIndicatorLight(eROBSONItems bit)
    {
        //Turn on/off the power indicators
        if (bit && bit.IndicatorLight)
        {
            bit.IndicatorLight.SwitchLight(bit.HasPower);
        }
    }



    /// <summary>
    /// The actions which any bit will do on active/deactive status
    /// </summary>
    /// <param name="active"></param>
    private static async void BitActionToggle(eROBSONItems bit, bool status)
    {
        //Turn on/off the power indicators
        SwitchPowerIndicatorLight(bit);

        var temp = CalculateValue(bit);
        var averageValue = temp == -1 ? 2 : temp * 2; //If no dimmable use default

        switch (bit.ID)
        {
            case BitID.I3BUTTON:
                break;
            case BitID.I5SLIDEDIMMER:
                break;
            case BitID.I11PRESSURESENSOR:
                break;
            case BitID.I18MOTIONSENSOR:
                break;
            case BitID.O2LONGLED:
                var light = bit.GetComponentInChildren<Light>();
                light.enabled = status;
                light.intensity = averageValue;

                //Change the material of the light bulb
                var lightMeshRenderer = light.GetComponentInParent<MeshRenderer>();
                Material lightMaterial;
                if (status)
                {
                    lightMaterial = await ReferenceLoader.GetAssetReferenceAsync<Material>("eROBSON/Materials/LightOnMaterial");
                }
                else
                {
                    lightMaterial = await ReferenceLoader.GetAssetReferenceAsync<Material>("eROBSON/Materials/LightOffMaterial");
                }

                lightMeshRenderer.material = lightMaterial;
                break;
            case BitID.O6BUZZER:
                break;
            case BitID.O9BARGRAPH:
                break;
            case BitID.O13FAN:
            case BitID.O25DCMOTOR:
                var anim = bit.GetComponent<Animator>();
                anim.SetBool("ON", status);
                anim.SetFloat("Speed", averageValue);
                break;
            case BitID.P3USBPOWERCONNECTOR:
                break;
            case BitID.W2BRANCH:
                break;
            case BitID.W7FORK:
                break;
            case BitID.USBPOWER:
                break;
            default:
                break;
        }
    }


    /// <summary>
    /// Move an item from "index" to "0" in a list
    /// </summary>
    /// <param name="list"></param>
    /// <param name="index"></param>
    private static void MoveToTop(List<eROBSONItems> list, int index)
    {
        var item = list[index];
        for (var i = index; i > 0; i--)
        {
            list[i] = list[i - 1];
        }
        list[0] = item;
    }

}
