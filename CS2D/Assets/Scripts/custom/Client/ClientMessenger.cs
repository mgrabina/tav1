using System.Collections.Generic;
using System.Linq;
using System.Net;
using custom.Network;
using custom.Utils;
using UnityEngine;

namespace custom.Client
{
    public class ClientMessenger : MonoBehaviour
    {
    
        // Networking
        [SerializeField] private GameObject clientCubePrefab;
        private HashSet<int> playerIds = new HashSet<int>();
        private List<CubeEntity> clientCubes;
        private List<Commands> commands = new List<Commands>();
        private List<Snapshot> interpolationBuffer = new List<Snapshot>();
        private MessageBuilder mb;

        
        // State Params
        public int id;
        private bool clientResponding = false, registered = false, connected = true, initialized = false;
        private float clientTime = 0f, accumulatedTime_c2 = 0f;
        private int packetNumber = 0, lastCommandLocallyExcecuted = 0;
        private Rigidbody myRigidbody, concilliateRB;
        
        private void Start()
        {
            id = generate_id();
            mb = new MessageBuilder(id, Constants.clients_base_port + id*10, Constants.server_base_port, Constants.serverIP);
            clientCubes = new List<CubeEntity>();
            if (!registered)
            {
                register();
            }
        }

        private void Update()
        {
            accumulatedTime_c2 += Time.deltaTime;
            
            getAndProcessMessage();

            if (Input.GetKeyDown(KeyCode.D))
            {
                connected = !connected;
            }
            
            if (registered && connected)
            {
                ReadInput();

                Predict();
                
                sendCommands();
        
                updateServerVisualization();
            }
        }

        private void getAndProcessMessage()
        {
            Message message;
            while ((message = mb.GETChannelMessage()) != null)
            {
                switch (message.GetType)
                {
                    case Message.Type.PLAYER_JOINED: processPlayerJoined((PlayerJoinedMessage) message); break;
                    case Message.Type.INIT_STATUS:
                        if (initialized)
                        {
                            return;
                        }
                        processInitStatus((InitStatusMessage) message); break;
                    case Message.Type.GAME_STATE_UPDATE:
                        if (!registered || !connected)
                        {
                            return;
                        }
                        processServerUpdate((ServerUpdateMessage) message); break;
                    case Message.Type.CLIENT_UPDATE_ACK: 
                        if (!registered || !connected)
                        {
                            return;
                        }
                        processServerACK((ServerACKMessage) message); break;
                }
            }
        }

        private void register()
        {
            mb.GenerateJoinGameMessage(id).Send();
        }

        private void processInitStatus(InitStatusMessage message)
        {
            initialized = !initialized;
            var snapshot = new Snapshot(-1, clientCubes);
            var buffer = message.Packet.buffer;
            snapshot.Deserialize(buffer, this);
            
            Snapshot.setUniqueSnapshot(snapshot).applyChanges(id);
        }
        
        private void processPlayerJoined(PlayerJoinedMessage message)
        {
            int idJoined = (message).IdJoined();
            if (playerIds.Contains(idJoined))
            {
                return;
            }
            GameObject clientCube = createClient(idJoined);
            if (idJoined == this.id)
            {
                registered = true;
                myRigidbody = clientCube.GetComponent<Rigidbody>();
                concilliateRB = Rigidbody.Instantiate(myRigidbody);
            }
        }

        private void processServerUpdate(ServerUpdateMessage message)
        {
            // Recieved
            var snapshot = new Snapshot(-1, clientCubes);
            var buffer = message.Packet.buffer;
            snapshot.Deserialize(buffer, this);

            int interpolationBufferSize = interpolationBuffer.Count;
            if (interpolationBufferSize == 0
                || snapshot.GetPacketNumber() > interpolationBuffer[interpolationBufferSize - 1].GetPacketNumber())
            {
                interpolationBuffer.Add(snapshot);
            }

            Concilliate();
        }
        
        private void updateServerVisualization()
        {
            // Interpolation
            if (interpolationBuffer.Count >= Constants.requiredSnapshots)
            {
                clientResponding = true;
            }
            else if (interpolationBuffer.Count <= 1)
            {
                clientResponding = false;
            }

            if (clientResponding)
            {
                clientTime += Time.deltaTime;
                Interpolate();
            }
        }

        private void sendCommands()
        {
            if (accumulatedTime_c2 >= Constants.sendRate)
            {
                mb.GenerateClientUpdateMessage().setArguments(commands).Send();
                accumulatedTime_c2 -= Constants.sendRate;
            }
        }

        private void processServerACK(ServerACKMessage message)
        {
            var toDelete = message.getNumber();
            while (commands.Count != 0)
            {
                if (commands[0].number <= toDelete || commands[0].timestamp < Time.time)
                {
                    commands.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }
        private void Interpolate()
        {
            var previousTime = (interpolationBuffer[0]).GetPacketNumber() * (1f / Constants.pps);
            var nextTime = interpolationBuffer[1].GetPacketNumber() * (1f / Constants.pps);
            var period = (clientTime - previousTime) / (nextTime - previousTime);
            var interpolatedSnapshot =
                Snapshot.createInterpolationSnapshot(interpolationBuffer[0], interpolationBuffer[1], period, id, this);
            interpolatedSnapshot.applyChanges(id);
    
            if (clientTime > nextTime)
            {
                interpolationBuffer.RemoveAt(0);
            }
        }

        private void ReadInput()
        {
            var timeout = Time.time + 2;
            var command = new Commands(packetNumber++, 
                Input.GetKeyDown(KeyCode.UpArrow), 
                Input.GetKeyDown(KeyCode.DownArrow),
                Input.GetKeyDown(KeyCode.LeftArrow),
                Input.GetKeyDown(KeyCode.RightArrow),
                Input.GetKeyDown(KeyCode.Space), timeout);
            if (command.notNull())
            {
                commands.Add(command);
            }
            else
            {
                packetNumber--;
            }
        }

        private void Predict()
        {
            foreach (Commands commands in commands)
            {
                if (commands.number > lastCommandLocallyExcecuted)
                {
                    lastCommandLocallyExcecuted = commands.number;
                    Vector3 force = Commands.generateForce(commands);                        
                    myRigidbody.AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);
                }
            }
        }

        private void Concilliate()
        {
            CubeEntity lastFromServer = interpolationBuffer.Last().getEntityById(id);
            concilliateRB.transform.position = lastFromServer.AuxPosition;
            concilliateRB.transform.rotation = lastFromServer.AuxRotation;
            int currentServerCommandExcecuted = lastFromServer.AuxLastCommandProcessed;
    
            foreach (var command in commands)    
            {
                if (currentServerCommandExcecuted < command.number)
                {
                    currentServerCommandExcecuted = command.number;
                    Vector3 force = Commands.generateForce(command);
                    concilliateRB.AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);
                }
            }

            myRigidbody.transform.position = concilliateRB.transform.position;
            myRigidbody.transform.rotation = concilliateRB.transform.rotation;
        }

        private static int generate_id()
        {
            return Random.Range(0, 100);
        }

        public GameObject createClient(int idJoined)
        {
            playerIds.Add(idJoined);
            var clientCube = Instantiate(clientCubePrefab, new Vector3(0, 0.5f, 0), new Quaternion());
            clientCubes.Add(new CubeEntity(clientCube, idJoined));
            return clientCube;
        }

        public bool isIdRegistered(int id)
        {
            return playerIds.Contains(id);
        }
        
        public void OnDestroy() {
            mb.Disconnect();
        }
    }
}
