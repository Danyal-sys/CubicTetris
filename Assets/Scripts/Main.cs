﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public GameObject blockPrefab;
    public GameObject hintPrefab;

	public GameObject camera;
	public CameraMovement cameraScript;


    public static General.Block[] blocks;

    public GameObject NextBlock;
    public GameObject FinishedCube;
    public GameObject GameArea;
    private GameObject[,,] space = new GameObject[2, General.length, General.height + 4];


    private GameObject currentBlockObject;
    private GameObject currentNextBlockObject = null;
    private BlockBase currentScript;
    private float timeForNextCheck;
    private bool isMoving = false;
    private bool allowRoate = true;
    private float timeForMovingAni;
    private int nextBlockId;

    private GameObject[,] hintboxes = new GameObject[2, 4];

	// Use this for initialization
	void Start () {

		camera = GameObject.FindGameObjectWithTag ("MainCamera");
		cameraScript = (CameraMovement)camera.GetComponent(typeof(CameraMovement));
        blocks = General.generateBlockTemplate();

        nextBlockId = Random.Range(0, blocks.Length);
        addNextBlock();

    }



    // hint boxes
    void createHintBoxes() {

        clearHintBoxes();

        for (int i = 0; i < 2; i++) {
            for (int k = 0; k < 4; k++) {

                bool flag = false;
                int j;
                for (j = 0; j < 4; j++) {
                    if (currentScript.block.block[i, j, k] > 0) {
                        flag = true;
                        break;
                    }
                }

                if (flag) {
                    // need a hint box at [i, k]
                    hintboxes[i, k] = Instantiate(hintPrefab);
                    hintboxes[i, k].transform.SetParent(GameArea.transform);

                    // actual position
                    int x, y, z;
                    x = (k + currentScript.x);
                    z = (i + currentScript.z);

                    //find first empty position
                    for (y = j + currentScript.y; y >= 0; y--) {
                        if (space[z, x, y] != null) {
                            break;
                        }
                    }
                    y += 1;

                    hintboxes[i, k].transform.localPosition = new Vector3(x, y, z) * General.cubeSize;
                    hintboxes[i, k].transform.localPosition += new Vector3(0.0f, -0.2f, 0.0f);

                    
                }
            }
        }
        
    }

    void clearHintBoxes() {
        for (int i = 0; i < 2; i++) {
            for (int k = 0; k < 4; k++) {
                if (hintboxes[i, k] != null) {
                    Destroy(hintboxes[i, k]);
                    hintboxes[i, k] = null;
                }
            }
        }
        
    }

    // ----------

    void addNextBlock() {

        // random block
        currentBlockObject = createBlock(this.gameObject, blocks[nextBlockId]);
		//currentBlockObject = createBlock(this.gameObject, blocks[8]);

        currentScript = (BlockBase)currentBlockObject.GetComponent(typeof(BlockBase));
        // random pos
        currentScript.x = Random.Range(currentScript.xMin, currentScript.xMax + 1);
        
        if (!needStop(currentScript.block.block, 0, 0, 1)) {
            if(Random.Range(0,2)==1) {
                currentScript.z += 1;
            }
        }
        currentScript.fixPositionZ();
        currentScript.fixPositionX();


        createHintBoxes();

        timeForNextCheck = General.timeForEachDrop;
        isMoving = true;
        allowRoate = true;

        if (currentNextBlockObject != null) {
            Destroy(currentNextBlockObject);
        }

        nextBlockId = Random.Range(0, blocks.Length);
        currentNextBlockObject = createBlock(this.gameObject, blocks[nextBlockId]);
        currentNextBlockObject.transform.parent = NextBlock.transform;
        currentNextBlockObject.transform.localPosition = new Vector3(0, 0, 0);

    }


    GameObject createBlock(GameObject playArea, General.Block block) {
        GameObject blockObject = Instantiate(blockPrefab);
        blockObject.transform.SetParent(playArea.transform);
        BlockBase script = (BlockBase) blockObject.GetComponent(typeof(BlockBase));
        script.block = block;
        script.createCubes();
        script.computeXRange();


        // temporary
        script.x = script.xMin;
        script.y = General.height;

        script.fixPositionX();
        script.fixPositionY();



        return blockObject;
    }

    // Update is called once per frame1

    bool isSpaceoccupied(int i, int j, int k) {
        if (!((0 <= i) && (i < 2))) return true;
        if (!((0 <= j) && (j < General.length))) return true;
        if (!((0 <= k) && (k < General.height + 4))) return true;
        if (space[i, j, k] != null) return true;

        return false;
    }


    public bool needStop(int[,,] block, int xOffset, int yOffset, int zOffset) {
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 4; j++) {
                for (int k = 0; k < 4; k++) {
                    if (block[i,j,k]!=0) {
                        if(isSpaceoccupied(i + currentScript.z + zOffset, k + currentScript.x + xOffset, j + currentScript.y + yOffset)) {
                            return true;
                        }
                    }
                }
            }
        }


        return false;

    }

    void finishCurrentBlock() {
        currentScript.fixPositionX();
        currentScript.fixPositionY();

        // add to space
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 4; j++) {
                for (int k = 0; k < 4; k++) {
                    if (currentScript.block.block[i, j, k] != 0) {
                        space[i + currentScript.z, k + currentScript.x, j + currentScript.y] = currentScript.cubes[currentScript.block.block[i, j, k]];
                    }
                }
            }
        }
        currentScript.setCubeParent(FinishedCube);
		transform.Find ("Main Camera").GetComponent<CameraShake> ().shake ();
    }

    int findFullRow() {
        for (int k = 0; k < General.height; k++) {
            // for each row
            bool rowFlag = true;
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < General.length; j++) {
                    if (space[i, j, k] == null) {
                        rowFlag = false;
                        break;
                    }
                }
                if (!rowFlag) break;
            }

            if(rowFlag) {
                return k;
            }

        }

        return -1;

    }


    void cleanFullRow() {

        while (true) {
            int row = findFullRow();
            if (row == -1) break;


            Score.score += 100;

            // delete
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < General.length; j++) {
                    Destroy(space[i, j, row]);
                }
            }

            // fall
            for (int k = row; k < General.height+4; k++) {
                // for each row
                for (int i = 0; i < 2; i++) {
                    for (int j = 0; j < General.length; j++) {
                        if(k==General.height+4-1) {
                            // clean top row
                            space[i, j, k] = null;
                        } else {
                            space[i, j, k] = space[i, j, k + 1];
                            if (space[i,j,k]!=null) {
                                space[i, j, k].transform.position -= new Vector3(0.0f, General.cubeSize, 0.0f);
                            }                            
                        }
                        

                    }
                }
            }
        }

    }


    void Update() {
        if (isMoving) {




            //  currentBlockObject.transform.position +=
            //      new Vector3(0.0f, -General.cubeSize * (Time.deltaTime / General.timeForEachMove), 0.0f);


            timeForNextCheck -= Time.deltaTime;


            if (timeForNextCheck <= 0) {
                timeForNextCheck += General.timeForEachDrop;

                if (needStop(currentScript.block.block, 0, -1, 0)) {
                    currentScript.fixPositionY();
                    isMoving = false;
                    finishCurrentBlock();
                    clearHintBoxes();
                    cleanFullRow();
                    addNextBlock();
                } else {
                    currentScript.y -= 1;
                    timeForMovingAni = 0;
                    if (needStop(currentScript.block.block, 0, -1, 0)) {
                        allowRoate = false;
                    }

                }



            } else {




                if (timeForMovingAni <= General.timeForEachMoveAni && timeForMovingAni >= 0) {
                    float yChange = -General.cubeSize * General.rubberBandFunction(timeForMovingAni / General.timeForEachMoveAni);

                    timeForMovingAni += Time.deltaTime;

                    yChange += General.cubeSize * General.rubberBandFunction(timeForMovingAni / General.timeForEachMoveAni);
                    currentBlockObject.transform.position
                                      += new Vector3(0.0f, -yChange, 0.0f);

                }

                if (timeForMovingAni > General.timeForEachMoveAni) {
                    currentScript.fixPositionY();
                    timeForMovingAni = -1.0f;
                }


				//cameraScript.isFlipped()
				//1 : back, A = right movement, D =  left movement
				//-1 : right A = left movement, D = right movement

				int left = cameraScript.isFlipped ();


                if (Input.GetKeyDown("space") && allowRoate) {
					currentScript.rotateRight(this);
                    createHintBoxes();
                }
                if (Input.GetKeyDown("a")) {
						if (!needStop(currentScript.block.block, left, 0, 0)) {
                        currentScript.x += left;
                        currentScript.fixPositionX();
                        createHintBoxes();
                    }
                }
                if (Input.GetKeyDown("d")) {
					if (!needStop(currentScript.block.block, -left, 0, 0)) {
                        currentScript.x -= left;
                        currentScript.fixPositionX();
                        createHintBoxes();
                    }
                }
                if (Input.GetKeyDown("s")) {
                    if (!needStop(currentScript.block.block, 0, 0, -1)) {
                        currentScript.z -= 1;
                        currentScript.fixPositionZ();
                        createHintBoxes();
                    }
                }
                if (Input.GetKeyDown("w")) {
                    if (!needStop(currentScript.block.block, 0, 0, 1)) {
                        currentScript.z += 1;
                        currentScript.fixPositionZ();
                        createHintBoxes();
                    }
                }
            }


        }



    }
}