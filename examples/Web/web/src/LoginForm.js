import React from 'react'
import { Button, Form, Grid, Header, Icon, Segment } from 'semantic-ui-react'

const LoginForm = () => (
    <Grid textAlign='center' style={{ height: '100vh' }} verticalAlign='middle'>
        <Grid.Column style={{ maxWidth: 360 }}>
            <Header as='h2' textAlign='center'>
                Soulseek Web Example
            </Header>
            <Form size='large'>
                <Segment>
                    <Form.Input 
                        fluid icon='user' 
                        iconPosition='left' 
                        placeholder='Username' 
                    />
                    <Form.Input
                        fluid
                        icon='lock'
                        iconPosition='left'
                        placeholder='Password'
                        type='password'
                    />
                    <Button 
                        primary 
                        fluid 
                        size='large'
                        className='login-button'
                    >
                        <Icon name='sign in'/>
                        Login
                    </Button>
                </Segment>
            </Form>
        </Grid.Column>
    </Grid>
)

export default LoginForm