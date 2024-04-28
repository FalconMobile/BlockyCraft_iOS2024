using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LoadSaveParamGame
{
    public static string nameNewGame = String.Empty;
    //Передаем название сохранения для загрузки 
    public static string nameLoadSaveMap = String.Empty;
    
    //Передаем название сохранения инвентаря для загрузки 
    public static InventoryUI PlayerInventoryUI {get; set;}
}
