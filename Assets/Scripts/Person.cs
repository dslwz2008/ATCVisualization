using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Person
{
    public int Id;
    public List<Position> Track = new List<Position>();
    public GameObject GameObject;
    public int CurrentIndex = 0;
}
