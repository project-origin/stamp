FROM rabbitmq:3.13-management

#RUN rabbitmq-plugins enable --offline rabbitmq_management

#RUN rabbitmqctl add_user guest guest
#RUN rabbitmqctl set_user_tags guest administrator

EXPOSE 15672
