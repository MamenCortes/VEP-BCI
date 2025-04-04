using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{

    //Creo una referencia a mi GameManager (y a todos sus metodos) que
    //como es estatico no necesito un GameObject para acceder a el pero si
    //una referencia 
    protected static T instance;

    //Para acceder a todos los metodos de GameManager, como es privado, genero la variable 
    //p�blica con lo que define el acceso con los getters y setters
    public static T Instance
    {
        get
        {
            if (instance == null) //Busco si existe alguno en la escena
            {
                instance = FindObjectOfType<T>(); //si lo hay, coge ese
                if (instance == null) //si no lo hay
                {
                    //lo creo
                    instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }
            }
            return instance;
        }
    }



}