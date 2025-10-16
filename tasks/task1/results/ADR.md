### <a name="_b7urdng99y53"></a>**ADR по рефакторингу приложения Hotelio на микросервисы:** 
### <a name="_hjk0fkfyohdk"></a>**Вячеслав Табачник:**
### <a name="_uanumrh8zrui"></a>**Дата:20 сентября 2025г**
### <a name="_3bfxc9a45514"></a>**Функциональные требования**


| **№** | **Действующие лица или системы**                         | **Use Case**                                                             | **Описание**                                                                                                                                                                            |
|:-----:|:---------------------------------------------------------|:-------------------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|   1   | New Microservice HotelService, API gateway               | Новый микросервис корректно обрабатывает запросы                         | Вызов ручек сервиса HotelService происходит в новом микросервисе и корректно выполняет смигрированные операции: getHotelById,  isOperational, isFullyBooked,findByCity,topRatedInCity   |
|   2   | Monolith, API gateway                                    | Существующий монолит корректно обрабатывает запросы к HotelController    | Вызов ручек  HotelioMonolith->GotelController корректно выполняет существующие операции: getHotelById,  isOperational, isFullyBooked,findByCity,topRatedInCity                          | 
|   3   | Monolith, New Microservice HotelService, API gateway     | Переключение с монолита на новый микросервис                             | Переключение происходит бесшовно, без паданий запросов и отказов в обслуживании                                                                                                         |
|   4   | Monolith, New Microservice HotelService, API gateway     | Переключение с нового микросервиса на монолит                            | Переключение происходит бесшовно, без паданий запросов и отказов в обслуживании                                                                                                         | 
|  5    | New Microservice HotelService                            | Миграция данных HotelService                                             | Существующие данные связанные с функионалом HotelService полностью смигрированные в БД нового микросервиса                                                                              | 


### <a name="_u8xz25hbrgql"></a>**Нефункциональные требования**

| **№** | **Требование**                                                                     |
|:-----:|:-----------------------------------------------------------------------------------|
|  U1   | Документирование апи нового микросервиса (Open API)                                |
|  R1   | Показатели SLA нового сервиса не менее заявленных для монолита                     |
|  P1   | Показатели метрик Response Type, Error Rate,  RPS на уровне существуюшего монолита |
|  S1   | Новая функциональность разрабатывается только в новом сервисе                      |
|  X1   | Kafka использование                                                                |
|  X1   | GraphQA  использование                                                             |

### <a name="_qmphm5d6rvi3"></a>**Решение**


Основная проблема - монолитное приложение,  низкая масштабируемость и сложность сопровождения 
Общие: Отсутсвие авторизации, сбора перформанс метрик, отсутствие автоматических recovery процедур,
Код: хардкор параметров в коде, нетранзакционный процесс оформления бронирования в Booking Service без возможности провести отката в случае ошибки и неконсистентного состояния системы


Предлается вынести функционал предоставляемый через API HotelController в отдельный миикросервис, как отличный кандидат по соотношению функций:
Техническиих Минимальные риски для первой миграции/бизнес значение - востребованное апи при работе приложение/ускорение загрузки информации об отелях для конечных пользователей

Выбор HotelService как первого функционала для миграции обусловлен:
1. Отсутствие внешних зависимостей - минимальный риск проблем при миграции
2. Понятный доменные границы - база данных отелей и связанные с ними функционал - локация , оценки итп
3. Относительно простая бизнес логика, простая модель данных
4. Высокая востребованность для бизнеса, скорость загрузки информации об отелях
5. Простая миграция данных

План миграции будет включать

1. Разработка микросервиса и перенос бизнес логики
2. Развертывание и настрока API Gateway
3. Миграция данных
4. Создание ACL в виде HotelService proxy в монолите для редиректа запросов от BookingService к новому микросервису HotelServive 
4. Переключение API Gateway на работу с новым микросервисом

``` plantuml

@startuml Hotelio C4 Container Diagram
!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Container.puml

title Hotelio C4 Container Diagram

Person(browser, "Browser", "Web Client")
Person(mobile, "Mobile App", "Mobile Client")

System_Boundary(c1, "Hotelio System") {
    Container(frontend, "Frontend", "React/Vue", "Single-page application")
    Container(api_gateway, "API Gateway", "Spring Gateway", "Request routing and load balancing")
    
    System_Boundary(monolith, "Hotelio Monolith Backend") {
        Container(app_user_service, "App User Service", "Java/Spring", "User Management Logic")
        Container(booking_service, "Booking Service", "Java/Spring", "Booking Processing Logic")
        Container(hotel_service, "Hotel Service", "Java/Spring", "Hotel Management Logic")
        Container(review_service, "Review Service", "Java/Spring", "Review Processing Logic")
        Container(promo_service, "Promo Code Service", "Java/Spring", "Promo Validation Logic")
    }
    
    Container(extracted_hotel_service, "Hotel Service", "Microservice", "Hotel Information Service")
}

ContainerDb(monolith_db, "Monolith Database", "PostgreSQL", "Main application database")
ContainerDb(hotel_db, "Hotel Service Database", "PostgreSQL", "Hotel service database")

' User interactions
browser --> frontend : Uses
mobile --> frontend : Uses
frontend --> api_gateway : HTTPS

' API Gateway routing
api_gateway --> app_user_service : HTTP
api_gateway --> booking_service : HTTP
api_gateway --> hotel_service : HTTP
api_gateway --> review_service : HTTP
api_gateway --> promo_service : HTTP
api_gateway --> extracted_hotel_service : HTTP

' Internal monolith dependencies
app_user_service --> booking_service : Uses
hotel_service --> booking_service : Uses
review_service --> booking_service : Uses
promo_service --> app_user_service : Uses

' Database connections
app_user_service --> monolith_db : Reads/Writes
booking_service --> monolith_db : Reads/Writes
hotel_service --> monolith_db : Reads/Writes
review_service --> monolith_db : Reads/Writes
promo_service --> monolith_db : Reads/Writes

extracted_hotel_service --> hotel_db : Reads/Writes

' Styling
skinparam backgroundColor #FFFFFF
skinparam defaultFontSize 10
skinparam component {
    BackgroundColor<<default>> #E8F5E8
    BorderColor #2E7D32
    FontColor #000000
}

skinparam database {
    BackgroundColor #FCE4EC
    BorderColor #C2185B
    FontColor #000000
}

skinparam person {
    BackgroundColor #E1F5FE
    BorderColor #01579B
    FontColor #000000
}

skinparam rectangle {
    BackgroundColor<<default>> #E8F5E8
    BorderColor #2E7D32
}

skinparam rectangle<<boundary>> {
    BackgroundColor #F3E5F5
    BorderColor #4A148C
}

@enduml

```

### <a name="_bjrr7veeh80c"></a>**Альтернативы**

Альтернативным кандидатом для первой миграции является ReviewService в качестве первого компонента

ReviewService также не обладает внешними зависимостями что упрощает процесс вынесения в микросервис, но 
Опишите здесь наиболее важные альтернативные решения.

**Недостатки, ограничения, риски**

Подробно опишите здесь

недостатки
ограничения
риски
- в случае отката к монолиту потребуется обратная синхронизация данных от микросервиса в бд монолита
- поддержка CI/CD  нового микросервиса ложится на единственного DevOps

