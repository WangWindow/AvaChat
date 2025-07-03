"""
AvaChat服务器数据库初始化脚本
添加测试用户、好友关系和消息数据
"""

import sqlite3
import os
from datetime import datetime, timedelta
import random


def get_db_path():
    """获取数据库路径"""
    appdata = os.environ.get("APPDATA")
    if not appdata:
        raise Exception("无法获取APPDATA环境变量")

    db_dir = os.path.join(appdata, "AvaChat")
    os.makedirs(db_dir, exist_ok=True)

    return os.path.join(db_dir, "AvaChat.Server.db")


def init_test_data():
    """初始化测试数据"""
    db_path = get_db_path()
    print(f"数据库路径: {db_path}")

    # 连接数据库
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    try:
        # 1. 添加测试用户
        print("添加测试用户...")
        users_data = [
            ("10000001", "Alice", "password123", 0, datetime.now()),  # Offline
            ("10000002", "Bob", "password123", 0, datetime.now()),  # Offline
            ("10000003", "Charlie", "password123", 1, datetime.now()),  # Online
            ("10000004", "Diana", "password123", 0, datetime.now()),  # Offline
            ("10000005", "Eva", "password123", 1, datetime.now()),  # Online
            ("10000006", "Frank", "password123", 0, datetime.now()),  # Offline
            ("10000007", "Grace", "password123", 1, datetime.now()),  # Online
            ("10000008", "Henry", "password123", 0, datetime.now()),  # Offline
            ("10000009", "Iris", "password123", 1, datetime.now()),  # Online
            ("10000010", "Jack", "password123", 0, datetime.now()),  # Offline
        ]

        # 清除现有用户数据
        cursor.execute("DELETE FROM Users")

        # 插入用户数据
        for user_id, username, password, status, last_login in users_data:
            cursor.execute(
                """
                INSERT OR REPLACE INTO Users (UserId, UserName, Password, Status, LastLoginTime)
                VALUES (?, ?, ?, ?, ?)
            """,
                (
                    user_id,
                    username,
                    password,
                    status,
                    last_login.strftime("%Y-%m-%d %H:%M:%S"),
                ),
            )

        print(f"添加了 {len(users_data)} 个测试用户")

        # 2. 添加好友关系
        print("添加好友关系...")
        friendships_data = [
            # Alice的好友
            (
                "10000001",
                "10000002",
                datetime.now() - timedelta(days=30),
            ),  # Alice - Bob
            (
                "10000001",
                "10000003",
                datetime.now() - timedelta(days=25),
            ),  # Alice - Charlie
            (
                "10000001",
                "10000004",
                datetime.now() - timedelta(days=20),
            ),  # Alice - Diana
            # Bob的好友
            (
                "10000002",
                "10000001",
                datetime.now() - timedelta(days=30),
            ),  # Bob - Alice (双向)
            ("10000002", "10000005", datetime.now() - timedelta(days=15)),  # Bob - Eva
            (
                "10000002",
                "10000006",
                datetime.now() - timedelta(days=10),
            ),  # Bob - Frank
            # Charlie的好友
            (
                "10000003",
                "10000001",
                datetime.now() - timedelta(days=25),
            ),  # Charlie - Alice (双向)
            (
                "10000003",
                "10000007",
                datetime.now() - timedelta(days=18),
            ),  # Charlie - Grace
            (
                "10000003",
                "10000008",
                datetime.now() - timedelta(days=12),
            ),  # Charlie - Henry
            # Diana的好友
            (
                "10000004",
                "10000001",
                datetime.now() - timedelta(days=20),
            ),  # Diana - Alice (双向)
            (
                "10000004",
                "10000009",
                datetime.now() - timedelta(days=8),
            ),  # Diana - Iris
            # Eva的好友
            (
                "10000005",
                "10000002",
                datetime.now() - timedelta(days=15),
            ),  # Eva - Bob (双向)
            ("10000005", "10000010", datetime.now() - timedelta(days=5)),  # Eva - Jack
            # Frank的好友
            (
                "10000006",
                "10000002",
                datetime.now() - timedelta(days=10),
            ),  # Frank - Bob (双向)
            # Grace的好友
            (
                "10000007",
                "10000003",
                datetime.now() - timedelta(days=18),
            ),  # Grace - Charlie (双向)
            # Henry的好友
            (
                "10000008",
                "10000003",
                datetime.now() - timedelta(days=12),
            ),  # Henry - Charlie (双向)
            # Iris的好友
            (
                "10000009",
                "10000004",
                datetime.now() - timedelta(days=8),
            ),  # Iris - Diana (双向)
            # Jack的好友
            (
                "10000010",
                "10000005",
                datetime.now() - timedelta(days=5),
            ),  # Jack - Eva (双向)
        ]

        # 清除现有好友关系
        cursor.execute("DELETE FROM Friendships")

        # 插入好友关系
        for user_id, friend_id, created_at in friendships_data:
            cursor.execute(
                """
                INSERT OR REPLACE INTO Friendships (UserId, FriendUserId, CreatedAt)
                VALUES (?, ?, ?)
            """,
                (user_id, friend_id, created_at.strftime("%Y-%m-%d %H:%M:%S")),
            )

        print(f"添加了 {len(friendships_data)} 个好友关系")

        # 3. 添加测试消息
        print("添加测试消息...")
        messages_data = []
        message_templates = [
            "你好！",
            "最近怎么样？",
            "今天天气真不错",
            "有空一起吃饭吗？",
            "工作顺利吗？",
            "周末有什么计划？",
            "谢谢你的帮助！",
            "晚安😴",
            "早上好！",
            "忙完了吗？",
            "这个项目进展如何？",
            "祝你好运！",
            "生日快乐！🎉",
            "新年快乐！",
            "路上小心",
            "到家了吗？",
            "记得吃饭哦",
            "加油！💪",
            "辛苦了",
            "明天见！",
        ]

        # 为每对好友生成一些消息
        message_id = 1
        for user_id, friend_id, _ in friendships_data:
            # 生成3-8条消息
            num_messages = random.randint(3, 8)
            for i in range(num_messages):
                # 随机选择发送方向
                if random.choice([True, False]):
                    sender, receiver = user_id, friend_id
                else:
                    sender, receiver = friend_id, user_id

                # 随机时间（最近7天内）
                timestamp = datetime.now() - timedelta(
                    days=random.randint(0, 7),
                    hours=random.randint(0, 23),
                    minutes=random.randint(0, 59),
                )

                content = random.choice(message_templates)

                messages_data.append(
                    (message_id, sender, receiver, content, timestamp)  # 直接使用数字ID
                )
                message_id += 1

        # 清除现有消息
        cursor.execute("DELETE FROM Messages")

        # 插入消息数据
        for msg_id, sender, receiver, content, timestamp in messages_data:
            cursor.execute(
                """
                INSERT OR REPLACE INTO Messages (MessageId, SenderId, ReceiverId, Content, Timestamp, Status, MessageType)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
                (
                    msg_id,
                    sender,
                    receiver,
                    content,
                    timestamp.strftime("%Y-%m-%d %H:%M:%S"),
                    0,  # Status: 0 = 已发送
                    0,  # MessageType: 0 = 文本消息
                ),
            )

        print(f"添加了 {len(messages_data)} 条测试消息")

        # 4. 添加一些好友请求
        print("添加好友请求...")
        friend_requests_data = [
            (
                1,  # req_id as integer
                "10000006",
                "10000007",
                "你好，我是Frank，想加你为好友",
                datetime.now() - timedelta(hours=2),
            ),
            (
                2,  # req_id as integer
                "10000008",
                "10000009",
                "嗨，我是Henry，可以加个好友吗？",
                datetime.now() - timedelta(hours=5),
            ),
            (
                3,  # req_id as integer
                "10000010",
                "10000001",
                "你好Alice，我是Jack",
                datetime.now() - timedelta(hours=1),
            ),
        ]

        # 清除现有好友请求
        cursor.execute("DELETE FROM FriendRequests")

        # 插入好友请求
        for req_id, from_user, to_user, message, timestamp in friend_requests_data:
            # 获取发送者的用户名
            cursor.execute("SELECT UserName FROM Users WHERE UserId = ?", (from_user,))
            from_user_name = cursor.fetchone()[0]

            cursor.execute(
                """
                INSERT OR REPLACE INTO FriendRequests (RequestId, FromUserId, ToUserId, FromUserName, Message, CreatedAt, Status)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
                (
                    req_id,
                    from_user,
                    to_user,
                    from_user_name,
                    message,
                    timestamp.strftime("%Y-%m-%d %H:%M:%S"),
                    0,  # Status: 0 = 待处理
                ),
            )

        print(f"添加了 {len(friend_requests_data)} 个好友请求")

        # 提交事务
        conn.commit()
        print("\n✅ 初始化数据添加完成！")

        # 显示统计信息
        print("\n📊 数据库统计:")
        cursor.execute("SELECT COUNT(*) FROM Users")
        user_count = cursor.fetchone()[0]
        print(f"   用户总数: {user_count}")

        cursor.execute("SELECT COUNT(*) FROM Users WHERE Status = 1")
        online_count = cursor.fetchone()[0]
        print(f"   在线用户: {online_count}")

        cursor.execute("SELECT COUNT(*) FROM Friendships")
        friendship_count = cursor.fetchone()[0]
        print(f"   好友关系: {friendship_count}")

        cursor.execute("SELECT COUNT(*) FROM Messages")
        message_count = cursor.fetchone()[0]
        print(f"   消息总数: {message_count}")

        cursor.execute("SELECT COUNT(*) FROM FriendRequests")
        request_count = cursor.fetchone()[0]
        print(f"   好友请求: {request_count}")

        print("\n🧪 测试账号信息:")
        print("   用户ID    用户名     密码        状态")
        print("   -------   -------   --------   ------")
        for user_id, username, password, status, _ in users_data:
            status_text = "在线" if status == 1 else "离线"
            print(f"   {user_id}   {username:<7}   {password:<8}   {status_text}")

    except Exception as e:
        print(f"❌ 错误: {e}")
        conn.rollback()
    finally:
        conn.close()


def clear_all_data():
    """清除所有数据"""
    db_path = get_db_path()
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    try:
        print("清除所有数据...")
        cursor.execute("DELETE FROM FriendRequests")
        cursor.execute("DELETE FROM Messages")
        cursor.execute("DELETE FROM Friendships")
        cursor.execute("DELETE FROM Users")
        conn.commit()
        print("✅ 数据清除完成！")
    except Exception as e:
        print(f"❌ 清除数据失败: {e}")
        conn.rollback()
    finally:
        conn.close()


if __name__ == "__main__":
    print("🚀 AvaChat 数据库初始化工具")
    print("=" * 50)

    while True:
        print("\n请选择操作:")
        print("1. 初始化测试数据")
        print("2. 清除所有数据")
        print("3. 退出")

        choice = input("\n请输入选项 (1-3): ").strip()

        if choice == "1":
            init_test_data()
        elif choice == "2":
            confirm = input("确定要清除所有数据吗？(输入 'yes' 确认): ").strip().lower()
            if confirm == "yes":
                clear_all_data()
            else:
                print("操作已取消")
        elif choice == "3":
            print("再见！")
            break
        else:
            print("无效选项，请重新选择")
