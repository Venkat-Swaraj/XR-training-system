using OrkestraLib;
using OrkestraLib.Exceptions;
using OrkestraLib.Message;
using OrkestraLib.Plugins;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ExampleOrkestra : Orkestra
{

    public InputField messageInput; 

    public Text userContext; 
    public Text appContext; 

    // Start is called before the first frame update
    void Start()
    {
     
        UserEvents += UserEventSubscriber;
        ApplicationEvents += AppEventSubscriber;

        // Register custom events to be used in the app
        RegisterEvents(new Type[]{
            typeof(ExampleOrkestraMessage),
        });

        OrkestraWithHSIO.Install(this, (graceful, message) =>
        {
            Events.Add(() =>
            {
                if (!graceful)
                {
                    Debug.Log(graceful);
                }
                else
                {
                    Debug.LogError("Error: "+message);
                }
            });
        });


        try
        {
            // Start Orkestra
            Connect(() =>
            {
                Debug.Log("All stuff is ready");
            });

        }
        catch (ServiceException e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void SendUserMessage()
    {
        //Send to the user I want
        //For the example we will send it to everyone except the actual user
        foreach(string userid in Users.Keys)
            if(userid!=agentId)
                Dispatch(Channel.User, new ExampleOrkestraMessage(this.agentId, messageInput.text),userid);
    }

    public void SendAppMessage()
    {
        Dispatch(Channel.Application, new ExampleOrkestraMessage(this.agentId, messageInput.text));
    }


    void UserEventSubscriber(object orkestraLib, UserEvent evt)
    {
        if (evt.IsPresenceEvent())
        {
            // Start session only when user is logged in
            if (evt.IsUser(agentId) && evt.IsJoinEvent())
            {
                Debug.Log("Logged as '" + evt.agentid + "' ");
                
            }
           
        }
        else if (evt.IsEvent(typeof(ExampleOrkestraMessage)))
        {
            ExampleOrkestraMessage eom = new ExampleOrkestraMessage(evt.data.value);
           
            Events.Add(() => {
                userContext.text = eom.message;
            });
           
        }
    }

    void AppEventSubscriber(object sender, ApplicationEvent evt)
    {
        if (evt.IsEvent(typeof(ExampleOrkestraMessage)))
        {
            ExampleOrkestraMessage eom = new ExampleOrkestraMessage(evt.value);

            if (eom.sender != agentId)
            {
                Events.Add(() => {
                    appContext.text = eom.message;
                });

            }

        }
    }
}