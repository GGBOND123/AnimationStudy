using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CameraController : MonoBehaviour
{
    private float m_deltX = 0f;
    private float m_deltY = 0f;
    //摄像机原始位置 和 旋转角度  给复原使用
    private Vector3 m_vecOriPosition;
    private Quaternion m_vecOriRotation;
    //手型工具：上次点击屏幕的位置
    private Vector3 m_vecLasMouseClickPosition;

    ///【2】用于控制幅度的变量
    //缩放幅度;
    public float m_fScalingSpeed = 10f;
    //镜头旋转幅度;
    public float m_fRotateSpeed = 5f;
    //手型工具幅度;
    public float m_fHandToolSpeed = -0.005f;
    Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>();
        m_vecOriRotation = camera.transform.rotation;
        m_vecOriPosition = camera.transform.position;
    }

    void Update()
    {
        //（1）旋转镜头 鼠标右键点下控制相机旋转;
        if (Input.GetMouseButton(1))
        {
            m_deltX += Input.GetAxis("Mouse X") * m_fRotateSpeed;
            m_deltY -= Input.GetAxis("Mouse Y") * m_fRotateSpeed;
            m_deltX = ClampAngle(m_deltX, -360, 360);
            m_deltY = ClampAngle(m_deltY, -70, 70);
            camera.transform.rotation = Quaternion.Euler(m_deltY, m_deltX, 0);
        }

        //（2）镜头缩放
        //鼠标中键点下场景缩放;
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            //自由缩放方式;
            m_fScalingSpeed = Input.GetAxis("Mouse ScrollWheel") * 10f;
            camera.transform.localPosition = camera.transform.position + camera.transform.forward * m_fScalingSpeed;
        }

        //（3）手型工具
        if (Input.GetMouseButtonDown(2))
        {
            m_vecLasMouseClickPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(2))
        {
            Vector3 NowHitPosition = Input.mousePosition;
            Vector3 offsetVec = NowHitPosition - m_vecLasMouseClickPosition;
            offsetVec = camera.transform.rotation * offsetVec;
            camera.transform.localPosition = camera.transform.localPosition + offsetVec * (m_fHandToolSpeed);
            m_vecLasMouseClickPosition = Input.mousePosition;
        }

        //(4)相机复位远点;
        if (Input.GetKey(KeyCode.Space))
        {
            m_deltX = 0f;
            m_deltY = 0f;
            m_deltX = ClampAngle(m_deltX, -360, 360);
            m_deltY = ClampAngle(m_deltY, -70, 70);
            m_fScalingSpeed = 10.0f;
            camera.transform.rotation = m_vecOriRotation;
            camera.transform.localPosition = m_vecOriPosition;
        }
    }

    //规划角度;
    float ClampAngle(float angle, float minAngle, float maxAgnle)
    {
        if (angle <= -360)
            angle += 360;
        if (angle >= 360)
            angle -= 360;

        return Mathf.Clamp(angle, minAngle, maxAgnle);
    }
}
